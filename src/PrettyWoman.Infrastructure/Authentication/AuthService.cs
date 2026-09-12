using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Infrastructure.Authentication;

public class AuthService(
    UserManager<User> userManager,
    IOptions<JwtOptions> jwtOptions,
    ApplicationDbContext context,
    RefreshTokenService refreshTokenService) : IAuthService
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly ApplicationDbContext _context = context;
    private readonly RefreshTokenService _refreshTokenService = refreshTokenService;

    public const string SecurityStampClaimType = "security_stamp";

    public async Task<AuthSessionDTO> LoginAsync(LoginRequestDTO loginRequest)
    {
        var user = await _userManager.FindByNameAsync(loginRequest.Username)
            ?? throw new AppUnauthorizedException("Credenciales invalidas.");

        if (!user.Enabled || await _userManager.IsLockedOutAsync(user))
        {
            throw new AppUnauthorizedException("Credenciales inválidas.");
        }

        if (!await _userManager.CheckPasswordAsync(user, loginRequest.Password))
        {
            await _userManager.AccessFailedAsync(user);
            throw new AppUnauthorizedException("Credenciales invalidas.");
        }

        // If password is correct before getting locked out, it resets the number of attempts
        if (await _userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        return await CreateSessionAsync(user);
    }

    public async Task<AuthSessionDTO> RefreshAsync(string refreshToken)
    {
        var (user, replacementRefreshToken) = await _refreshTokenService.RotateAsync(refreshToken);
        return new AuthSessionDTO { Response = await CreateAuthResponseAsync(user), RefreshToken = replacementRefreshToken };
    }

    public Task LogoutAsync(string refreshToken) => _refreshTokenService.RevokeAsync(refreshToken);

    public async Task<UserDTO> CreateUserAsync(CreateUserDTO createUserRequest)
    {
        if (!AppRoles.All.Contains(createUserRequest.Role))
        {
            throw new AppBadRequestException("El rol indicado no es valido.");
        }

        var user = new User
        {
            UserName = createUserRequest.Username,
            Email = createUserRequest.Email,
            EmailConfirmed = true,
            LockoutEnabled = true,
            Name = createUserRequest.Name,
            Lastname = createUserRequest.Lastname
        };

        var createResult = await _userManager.CreateAsync(user, createUserRequest.Password);

        if (!createResult.Succeeded)
        {
            throw new AppBadRequestException(
                string.Join(", ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, createUserRequest.Role);

        if (!roleResult.Succeeded)
        {
            throw new AppBadRequestException(
                string.Join(", ", roleResult.Errors.Select(error => error.Description)));
        }

        return await CreateUserDtoAsync(user);
    }

    public async Task<UserDTO> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new AppNotFoundException($"El usuario con id '{id}' no existe.");

        return await CreateUserDtoAsync(user);
    }

    public async Task<IReadOnlyCollection<UserDTO>> GetUsersAsync(
        string? user = null,
        string? role = null,
        bool? enabled = null)
    {
        var usersQuery = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(user))
        {
            var normalizedUser = user.Trim().ToUpperInvariant();
            usersQuery = usersQuery.Where(item =>
                (item.NormalizedUserName != null && item.NormalizedUserName.Contains(normalizedUser)) ||
                (item.NormalizedEmail != null && item.NormalizedEmail.Contains(normalizedUser)) ||
                item.Name.ToUpper().Contains(normalizedUser) ||
                item.Lastname.ToUpper().Contains(normalizedUser));
        }

        if (enabled.HasValue)
        {
            usersQuery = usersQuery.Where(item => item.Enabled == enabled.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToUpperInvariant();
            var roleId = await _context.Roles
                .Where(item => item.NormalizedName == normalizedRole)
                .Select(item => item.Id)
                .SingleOrDefaultAsync();

            if (roleId is null)
            {
                return Array.Empty<UserDTO>();
            }

            usersQuery = usersQuery.Where(item =>
                _context.UserRoles.Any(userRole => userRole.UserId == item.Id && userRole.RoleId == roleId));
        }

        var users = await usersQuery
            .OrderBy(account => account.Name)
            .ThenBy(account => account.Lastname)
            .ThenBy(account => account.UserName)
            .ToListAsync();

        var userDtos = new List<UserDTO>(users.Count);
        foreach (var account in users)
        {
            userDtos.Add(await CreateUserDtoAsync(account));
        }

        return userDtos;
    }

    public async Task<UserDTO> UpdateUserAsync(string id, UpdateUserDTO updateUserRequest)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new AppNotFoundException($"El usuario con id '{id}' no existe.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        user.Name = updateUserRequest.Name;
        user.Lastname = updateUserRequest.Lastname;
        user.Email = updateUserRequest.Email;
        var updateResult = await _userManager.UpdateAsync(user);
        EnsureSucceeded(updateResult);

        if (!string.IsNullOrWhiteSpace(updateUserRequest.Password))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, updateUserRequest.Password);
            EnsureSucceeded(passwordResult);
            await _refreshTokenService.RevokeAllForUserAsync(user.Id);
        }

        await transaction.CommitAsync();

        return await CreateUserDtoAsync(user);
    }

    public async Task<UserDTO> UnlockUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new AppNotFoundException($"El usuario con id '{id}' no existe.");

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return await CreateUserDtoAsync(user);
    }

    public async Task<UserDTO> DisableUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new AppNotFoundException($"El usuario con id '{id}' no existe.");

        await SetUserEnabledAsync(user, false);

        return await CreateUserDtoAsync(user);
    }

    public async Task<UserDTO> EnableUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new AppNotFoundException($"El usuario con id '{id}' no existe.");

        await SetUserEnabledAsync(user, true);

        return await CreateUserDtoAsync(user);
    }

    private async Task<AuthResponseDTO> CreateAuthResponseAsync(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.Name),
            new(SecurityStampClaimType, user.SecurityStamp ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthResponseDTO
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc,
            User = await CreateUserDtoAsync(user)
        };
    }

    private async Task<AuthSessionDTO> CreateSessionAsync(User user) => new()
    {
        Response = await CreateAuthResponseAsync(user),
        RefreshToken = await _refreshTokenService.CreateAsync(user)
    };

    private async Task<UserDTO> CreateUserDtoAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserDTO
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Lastname = user.Lastname,
            Enabled = user.Enabled,
            Locked = await _userManager.IsLockedOutAsync(user),
            Roles = roles.ToArray()
        };
    }

    private async Task UpdateSecurityStampAsync(User user)
    {
        var result = await _userManager.UpdateSecurityStampAsync(user);
        EnsureSucceeded(result);
    }

    private async Task SetUserEnabledAsync(User user, bool enabled)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        user.Enabled = enabled;
        var updateResult = await _userManager.UpdateAsync(user);
        EnsureSucceeded(updateResult);
        await UpdateSecurityStampAsync(user);
        await _refreshTokenService.RevokeAllForUserAsync(user.Id);

        await transaction.CommitAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new AppBadRequestException(
                string.Join(", ", result.Errors.Select(error => error.Description)));
        }
    }
}
