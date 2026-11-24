using Microsoft.EntityFrameworkCore;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Infrastructure.Authentication;

public static class RefreshTokenMaintenance
{
    public const int BatchSize = 1000;

    // Transaction-scoped PostgreSQL lock shared by rotations and exclusive to cleanup.
    private const long CleanupLock = 672905143875146019;

    internal static Task LockForRotationAsync(ApplicationDbContext context) =>
        context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({CleanupLock})");

    public static async Task<int?> TryDeleteExpiredBatchAsync(
        ApplicationDbContext context, DateTime cutoff, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Refresh token cleanup requires an active transaction.");

        await context.Database.ExecuteSqlRawAsync("SET LOCAL statement_timeout = '2s'", cancellationToken);
        var acquired = await context.Database.SqlQuery<bool>(
            $"SELECT pg_try_advisory_xact_lock({CleanupLock}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!acquired)
            return null;

        // Keep expired ancestors while a family can still refresh so reuse can
        // still be detected and the corresponding session can be revoked.
        var activeFamilies = context.RefreshTokens
            .Where(token => token.ExpiresAtUtc > cutoff && token.RevokedAtUtc == null)
            .Select(token => token.FamilyId);

        return await context.RefreshTokens
            .Where(token => token.ExpiresAtUtc <= cutoff && !activeFamilies.Contains(token.FamilyId))
            .OrderBy(token => token.ExpiresAtUtc)
            .ThenBy(token => token.Id)
            .Take(BatchSize)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
