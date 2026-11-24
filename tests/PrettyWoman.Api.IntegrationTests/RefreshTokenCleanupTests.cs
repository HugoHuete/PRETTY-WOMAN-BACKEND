using System.Data.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Infrastructure.Authentication;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class RefreshTokenCleanupTests(PrettyWomanApiFactory factory)
{
    [Fact]
    public async Task CleanupDefersDuringRotationAndPreservesTheExpiredAncestorOfItsReplacement()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;
        await using var scope = factory.Services.CreateAsyncScope();
        var observer = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await observer.Users.SingleAsync(user => user.UserName == PrettyWomanApiFactory.AdminUsername, cancellationToken);
        var protection = new EphemeralDataProtectionProvider();
        var tokenService = new RefreshTokenService(observer, protection);
        var rawToken = await tokenService.CreateAsync(user);
        var token = observer.RefreshTokens.Local.Single();
        await tokenService.CreateAsync(user);
        var inactiveToken = observer.RefreshTokens.Local.Single(row => row.FamilyId != token.FamilyId);
        inactiveToken.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        token.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        await observer.SaveChangesAsync(cancellationToken);
        var cutoff = token.ExpiresAtUtc.AddSeconds(1);
        var connectionString = observer.Database.GetConnectionString();

        var pause = new PauseRotationUpdate(cancellationToken);
        await using var rotationContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString).UseSnakeCaseNamingConvention().AddInterceptors(pause).Options);
        await using var cleanupContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString).UseSnakeCaseNamingConvention().Options);
        var rotation = new RefreshTokenService(rotationContext, protection).RotateAsync(rawToken);
        try
        {
            await pause.Started.Task.WaitAsync(cancellationToken);
            await using var deferredTransaction = await cleanupContext.Database.BeginTransactionAsync(cancellationToken);
            var deferred = await RefreshTokenMaintenance.TryDeleteExpiredBatchAsync(cleanupContext, cutoff, cancellationToken);
            Assert.Null(deferred);
        }
        finally
        {
            pause.Resume.TrySetResult();
            await rotation.WaitAsync(cancellationToken);
        }

        await using var cleanupTransaction = await cleanupContext.Database.BeginTransactionAsync(cancellationToken);
        var deleted = await RefreshTokenMaintenance.TryDeleteExpiredBatchAsync(cleanupContext, cutoff, cancellationToken);
        Assert.True(deleted is > 0);
        await cleanupTransaction.CommitAsync(cancellationToken);

        Assert.False(await observer.RefreshTokens.AnyAsync(row => row.Id == inactiveToken.Id, cancellationToken));
        var family = await observer.RefreshTokens.AsNoTracking()
            .Where(row => row.FamilyId == token.FamilyId)
            .ToListAsync(cancellationToken);
        Assert.Equal(2, family.Count);
        var ancestor = Assert.Single(family, row => row.Id == token.Id);
        Assert.NotNull(ancestor.RevokedAtUtc);
        Assert.Contains(family, row => row.Id == ancestor.ReplacedByTokenId && row.RevokedAtUtc == null);
    }

    private sealed class PauseRotationUpdate(CancellationToken cancellationToken) : DbCommandInterceptor
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken commandCancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE refresh_tokens", StringComparison.Ordinal))
            {
                Started.TrySetResult();
                await Resume.Task.WaitAsync(cancellationToken);
            }

            return result;
        }
    }
}
