using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PrettyWoman.Infrastructure.Authentication;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Workers;

public class Worker(ApplicationDbContext context, ILogger<Worker> logger)
{
    private static readonly TimeSpan TokenCleanupBudget = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TokenLockRetryDelay = TimeSpan.FromMilliseconds(100);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow;
        var expiredTokens = await DeleteExpiredTokensAsync(cutoff, cancellationToken);

        // Catalog cleanup can wait on its own locks without blocking token rotation.
        var expiredSelections = await context.SharedCatalogSelections
            .Where(selection => selection.ExpiresAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation(
            "Limpieza completada. Refresh tokens eliminados: {ExpiredTokens}. Selecciones eliminadas: {ExpiredSelections}.",
            expiredTokens,
            expiredSelections);
    }

    private async Task<int> DeleteExpiredTokensAsync(DateTime cutoff, CancellationToken cancellationToken)
    {
        var elapsed = Stopwatch.StartNew();
        var total = 0;
        while (elapsed.Elapsed < TokenCleanupBudget)
        {
            try
            {
                int? deleted;
                await using (var transaction = await context.Database.BeginTransactionAsync(cancellationToken))
                {
                    deleted = await RefreshTokenMaintenance.TryDeleteExpiredBatchAsync(context, cutoff, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }

                if (deleted is null)
                {
                    var remaining = TokenCleanupBudget - elapsed.Elapsed;
                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining < TokenLockRetryDelay ? remaining : TokenLockRetryDelay, cancellationToken);
                    continue;
                }

                total += deleted.Value;
                if (deleted < RefreshTokenMaintenance.BatchSize)
                    return total;
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.QueryCanceled && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Limpieza de refresh tokens pospuesta al superar el tiempo límite de PostgreSQL.");
                return total;
            }
        }

        logger.LogInformation("Limpieza de refresh tokens pausada al alcanzar el tiempo asignado a esta ejecución.");
        return total;
    }
}
