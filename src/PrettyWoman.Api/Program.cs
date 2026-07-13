using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PrettyWoman.Api.Middlewares;
using PrettyWoman.Api.Health;
using PrettyWoman.Application;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Infrastructure;
using PrettyWoman.Infrastructure.Authentication;
using PrettyWoman.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
const string AdminFrontendCorsPolicy = "AdminFrontend";

var configuredAdminOrigins = builder.Configuration.GetSection("Cors:AdminOrigins").Get<string[]>() ?? [];
var adminOrigins = configuredAdminOrigins.Length > 0
    ? configuredAdminOrigins
    : builder.Environment.IsDevelopment() ? ["http://localhost:5173"] : [];
if (adminOrigins.Length == 0 || adminOrigins.Any(string.IsNullOrWhiteSpace))
{
    throw new InvalidOperationException("Debe configurar al menos un origen en Cors:AdminOrigins.");
}

builder.Services.AddDataProtection();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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
            .AllowAnyMethod());
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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.RequireAdminRole, policy =>
        policy.RequireRole(AppRoles.Admin));

    options.AddPolicy(AppPolicies.RequireEmployeeRole, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Employee));

    options.FallbackPolicy = options.GetPolicy(AppPolicies.RequireEmployeeRole);
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready"]);

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services, app.Configuration);

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

app.Run();

public partial class Program
{
}

