using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.Health;

public sealed class PostgreSqlHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL está disponible.")
                : HealthCheckResult.Unhealthy("No se pudo establecer conexión con PostgreSQL.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("No se pudo establecer conexión con PostgreSQL.", exception);
        }
    }
}
