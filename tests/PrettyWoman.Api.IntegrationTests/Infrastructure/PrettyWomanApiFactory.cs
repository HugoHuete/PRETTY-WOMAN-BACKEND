using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PrettyWoman.Api.IntegrationTests.Infrastructure;

public sealed class PrettyWomanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin.integration@prettywoman.test";
    public const string AdminPassword = "Admin123!Integration";
    public const string EmployeeEmail = "employee.integration@prettywoman.test";
    public const string EmployeePassword = "Employee123!Integration";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pretty_woman_integration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    private readonly Dictionary<string, string?> _originalEnvironmentVariables = new();

    public PrettyWomanApiFactory()
    {
        SetEnvironmentVariable("Jwt__Key", "integration-tests-jwt-key-that-is-longer-than-thirty-two-characters");
        SetEnvironmentVariable("Jwt__Issuer", "PrettyWoman.IntegrationTests");
        SetEnvironmentVariable("Jwt__Audience", "PrettyWoman.IntegrationTests.Client");
        SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        SetEnvironmentVariable("SeedAdmin__Email", AdminEmail);
        SetEnvironmentVariable("SeedAdmin__Password", AdminPassword);
        SetEnvironmentVariable("SeedAdmin__Name", "Admin");
        SetEnvironmentVariable("SeedAdmin__Lastname", "Integration");
        SetEnvironmentVariable("Cors__AdminOrigins__0", "http://localhost:5173");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        foreach (var (name, value) in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        await _postgres.DisposeAsync();
        Dispose();
    }

    public async Task EnsureEmployeeAsync()
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = await userManager.FindByEmailAsync(EmployeeEmail);
        if (user is not null)
        {
            return;
        }

        user = new User
        {
            UserName = EmployeeEmail,
            Email = EmployeeEmail,
            EmailConfirmed = true,
            Name = "Empleado",
            Lastname = "Integracion",
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, EmployeePassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Employee);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(error => error.Description)));
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    private void SetEnvironmentVariable(string name, string value)
    {
        _originalEnvironmentVariables[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }
}
