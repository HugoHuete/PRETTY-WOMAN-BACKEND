using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Infrastructure.Authentication;

public class RefreshTokenService(
    ApplicationDbContext context,
    IDataProtectionProvider dataProtectionProvider)
{
    private const string ReplayProtectorPurpose = "PrettyWoman.RefreshToken.Replay.v1";
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromSeconds(5);
    private readonly ApplicationDbContext _context = context;
    private readonly IDataProtector _replayTokenProtector = dataProtectionProvider
        .CreateProtector(ReplayProtectorPurpose);

    public async Task<string> CreateAsync(User user)
    {
        var rawToken = CreateRawToken();
        _context.RefreshTokens.Add(CreateToken(user, rawToken, Guid.NewGuid()));
        await _context.SaveChangesAsync();
        return rawToken;
    }

    public async Task<(User User, string RefreshToken)> RotateAsync(string rawToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        var token = await _context.RefreshTokens.Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == Hash(rawToken))
            ?? throw new AppUnauthorizedException("Credenciales invalidas.");

        if (!IsUsable(token, now))
        {
            var replay = await TryReplayAsync(token, now);
            if (replay is not null)
            {
                await transaction.CommitAsync();
                return replay.Value;
            }

            await RevokeFamilyAsync(token.FamilyId);
            await transaction.CommitAsync();
            throw new AppUnauthorizedException("Credenciales invalidas.");
        }

        var replacementRawToken = CreateRawToken();
        var replacement = CreateToken(token.User, replacementRawToken, token.FamilyId);
        var revokedCount = await _context.RefreshTokens
            .Where(current => current.Id == token.Id && current.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(current => current.RevokedAtUtc, now)
                .SetProperty(current => current.ReplacedByTokenId, replacement.Id)
                // Guardamos el sucesor protegido solo hasta que expire la ventana.
                // Así una pestaña retrasada puede recibir la misma cookie sin crear otra rotación.
                .SetProperty(current => current.ReplacementTokenProtected,
                    _replayTokenProtector.Protect(replacementRawToken))
                .SetProperty(current => current.ReplayUntilUtc, now.Add(ReplayWindow)));
        if (revokedCount == 0)
        {
            // Otra solicitud ganó la carrera. Leemos sin tracking para observar el estado
            // confirmado y reutilizar su reemplazo si todavía está dentro de la tolerancia.
            var concurrentlyRotatedToken = await _context.RefreshTokens.AsNoTracking()
                .Include(current => current.User)
                .SingleAsync(current => current.TokenHash == Hash(rawToken));
            var replay = await TryReplayAsync(concurrentlyRotatedToken, now);
            if (replay is not null)
            {
                await transaction.CommitAsync();
                return replay.Value;
            }

            await RevokeFamilyAsync(token.FamilyId);
            await transaction.CommitAsync();
            throw new AppUnauthorizedException("Credenciales invalidas.");
        }

        _context.RefreshTokens.Add(replacement);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (token.User, replacementRawToken);
    }

    public async Task RevokeAsync(string rawToken)
    {
        var token = await _context.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == Hash(rawToken));
        if (token is not null)
        {
            // Cerrar sesión debe invalidar también un sucesor emitido durante la ventana de replay.
            await RevokeFamilyAsync(token.FamilyId);
        }
    }

    public Task RevokeAllForUserAsync(string userId) => _context.RefreshTokens
        .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
        .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, DateTime.UtcNow));

    private Task RevokeFamilyAsync(Guid familyId) => _context.RefreshTokens
        .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
        .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, DateTime.UtcNow));

    private async Task<(User User, string RefreshToken)?> TryReplayAsync(RefreshToken token, DateTime now)
    {
        if (token.RevokedAtUtc is null || token.ReplayUntilUtc < now ||
            token.ReplacedByTokenId is null || string.IsNullOrWhiteSpace(token.ReplacementTokenProtected) ||
            token.ExpiresAtUtc <= now || !token.User.Enabled ||
            token.User.SecurityStamp != token.SecurityStamp)
        {
            return null;
        }

        var replacementIsActive = await _context.RefreshTokens.AsNoTracking()
            .AnyAsync(current => current.Id == token.ReplacedByTokenId &&
                current.FamilyId == token.FamilyId &&
                current.RevokedAtUtc == null &&
                current.ExpiresAtUtc > now);
        if (!replacementIsActive) return null;

        try
        {
            return (token.User, _replayTokenProtector.Unprotect(token.ReplacementTokenProtected));
        }
        catch (CryptographicException)
        {
            // Si el valor protegido ya no se puede leer, no es seguro aceptar el refresh anterior.
            return null;
        }
    }

    private static bool IsUsable(RefreshToken token, DateTime now) =>
        token.RevokedAtUtc is null && token.ExpiresAtUtc > now && token.User.Enabled &&
        token.User.SecurityStamp == token.SecurityStamp;

    private static RefreshToken CreateToken(User user, string rawToken, Guid familyId) => new()
    {
        Id = Guid.NewGuid(),
        FamilyId = familyId,
        UserId = user.Id,
        User = user,
        TokenHash = Hash(rawToken),
        SecurityStamp = user.SecurityStamp ?? string.Empty,
        CreatedAtUtc = DateTime.UtcNow,
        ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
    };

    private static string CreateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
    private static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}
