using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Infrastructure.Authentication;

public class RefreshTokenService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<string> CreateAsync(User user)
    {
        var rawToken = CreateRawToken();
        _context.RefreshTokens.Add(CreateToken(user, rawToken));
        await _context.SaveChangesAsync();
        return rawToken;
    }

    public async Task<(User User, string RefreshToken)> RotateAsync(string rawToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var token = await _context.RefreshTokens.Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == Hash(rawToken))
            ?? throw new AppUnauthorizedException("Credenciales invalidas.");

        if (token.RevokedAtUtc is not null || token.ExpiresAtUtc <= DateTime.UtcNow ||
            !token.User.Enabled || token.User.SecurityStamp != token.SecurityStamp)
        {
            await RevokeAllForUserAsync(token.UserId);
            await transaction.CommitAsync();
            throw new AppUnauthorizedException("Credenciales invalidas.");
        }

        var replacementRawToken = CreateRawToken();
        var replacement = CreateToken(token.User, replacementRawToken);
        var revokedCount = await _context.RefreshTokens
            .Where(current => current.Id == token.Id && current.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(current => current.RevokedAtUtc, DateTime.UtcNow)
                .SetProperty(current => current.ReplacedByTokenId, replacement.Id));
        if (revokedCount == 0)
        {
            await RevokeAllForUserAsync(token.UserId);
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
        if (token is not null && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public Task RevokeAllForUserAsync(string userId) => _context.RefreshTokens
        .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
        .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, DateTime.UtcNow));

    private static RefreshToken CreateToken(User user, string rawToken) => new()
    {
        Id = Guid.NewGuid(),
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
