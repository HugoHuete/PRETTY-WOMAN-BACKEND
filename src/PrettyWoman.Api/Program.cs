using System.Text;
using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PrettyWoman.Api.Middlewares;
using PrettyWoman.Api.Health;
using PrettyWoman.Api.RateLimiting;
using PrettyWoman.Application;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Infrastructure;
using PrettyWoman.Infrastructure.Authentication;
using PrettyWoman.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
const string AdminFrontendCorsPolicy = "AdminFrontend";

if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var railwayPort)
    && railwayPort is > 0 and <= 65535)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

var configuredAdminOrigins = builder.Configuration.GetSection("Cors:AdminOrigins").Get<string[]>() ?? [];
var adminOrigins = configuredAdminOrigins.Length > 0
    ? configuredAdminOrigins
    : builder.Environment.IsDevelopment() ? ["http://localhost:5173"] : [];
if (adminOrigins.Length == 0 || adminOrigins.Any(string.IsNullOrWhiteSpace))
{
    throw new InvalidOperationException("Debe configurar al menos un origen en Cors:AdminOrigins.");
}

var rateLimitOptions = builder.Configuration.GetSection(ApiRateLimitOptions.SectionName).Get<ApiRateLimitOptions>()
    ?? new ApiRateLimitOptions();
rateLimitOptions.Validate();

builder.Services.AddDataProtection();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("100.0.0.0"), 8));
});
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    document
                )
            ] = []
        });
});

builder.Services.AddRouting(options => { options.LowercaseUrls = true; });
builder.Services.AddCors(options =>
{
    options.AddPolicy(AdminFrontendCorsPolicy, policy =>
        policy.WithOrigins(adminOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration.GetSection("AutoMapperLicense").Get<string>();
}, typeof(Program).Assembly, typeof(PrettyWoman.Application.DependencyInjection).Assembly);

builder.Services.AddIdentityCore<User>(options =>
    {
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("La configuracion Jwt es requerida.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("La configuracion Jwt:Key es requerida.");
}

if (jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("La configuracion Jwt:Key debe tener al menos 32 caracteres.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
        // En cada llamada a una api se hace una consulta a la base de datos para obtener el usuario y verificar estos valores. 
        // Esto es necesario para que cuando un usuario sea deshabilitado o se cambie su contraseña, los tokens existentes sean invalidados.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var securityStamp = context.Principal?.FindFirst(AuthService.SecurityStampClaimType)?.Value;
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                var user = userId is null ? null : await userManager.FindByIdAsync(userId);

                if (user is null || !user.Enabled || user.SecurityStamp != securityStamp)
                {
                    context.Fail("El token ya no es valido.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.RequireAdminRole, policy =>
        policy.RequireRole(AppRoles.Admin));

    options.AddPolicy(AppPolicies.RequireEmployeeRole, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Employee));

    options.FallbackPolicy = options.GetPolicy(AppPolicies.RequireEmployeeRole);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        var (name, permitLimit) = GetRateLimit(httpContext, rateLimitOptions);

        return RateLimitPartition.GetSlidingWindowLimiter(
            name,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds),
                SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "application/problem+json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Demasiadas solicitudes",
            Detail = "Intenta nuevamente más tarde."
        }, cancellationToken);
    };
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready"]);

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services, app.Configuration);

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(AdminFrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("live")
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready")
}).AllowAnonymous();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready")
}).AllowAnonymous();

app.Run();

static (string Name, int PermitLimit) GetRateLimit(HttpContext httpContext, ApiRateLimitOptions options)
{
    var request = httpContext.Request;
    var normalizedPath = request.Path.Value?.TrimEnd('/') ?? string.Empty;
    if (request.Method == HttpMethods.Post && string.Equals(normalizedPath, "/api/v1/auth/login", StringComparison.OrdinalIgnoreCase))
    {
        return ("login", options.LoginPermitLimit);
    }

    if (request.Path.Value?.Contains("/images", StringComparison.OrdinalIgnoreCase) == true
        && !HttpMethods.IsGet(request.Method)
        && !HttpMethods.IsHead(request.Method))
    {
        return ("images", options.ImagePermitLimit);
    }

    return HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
        ? ("read", options.ReadPermitLimit)
        : ("write", options.WritePermitLimit);
}

public partial class Program
{
}
