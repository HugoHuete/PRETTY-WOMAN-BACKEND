using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
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
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginRequest)
    {
        var user = await _userManager.FindByEmailAsync(loginRequest.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password))
        {
            throw new AppUnauthorizedException("Credenciales invalidas.");
        }

        return await CreateAuthResponseAsync(user);
    }

    public async Task<UserDTO> CreateUserAsync(CreateUserDTO createUserRequest)
    {
        if (!AppRoles.All.Contains(createUserRequest.Role))
        {
            throw new AppBadRequestException("El rol indicado no es valido.");
        }

        var user = new User
        {
            UserName = createUserRequest.Email,
            Email = createUserRequest.Email,
            EmailConfirmed = true,
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

    private async Task<AuthResponseDTO> CreateAuthResponseAsync(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.Name)
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

    private async Task<UserDTO> CreateUserDtoAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserDTO
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Lastname = user.Lastname,
            Roles = roles.ToArray()
        };
    }
}
