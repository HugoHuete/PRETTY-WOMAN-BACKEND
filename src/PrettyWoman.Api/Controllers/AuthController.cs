using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Claims;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Application.Exceptions;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger,
    IWebHostEnvironment environment) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IWebHostEnvironment _environment = environment;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginRequestDTO loginRequest)
    {
        var session = await _authService.LoginAsync(loginRequest);
        session.Response.CsrfToken = SetSessionCookies(session.RefreshToken);
        return Ok(session.Response);
    }

    [AllowAnonymous]
    // Las pestañas nuevas usan este endpoint para recuperar el CSRF de la sesión existente
    // antes de solicitar un access token. No genera ni rota credenciales.
    [HttpGet("csrf")]
    public ActionResult<object> GetCsrfToken()
    {
        if (!Request.Cookies.TryGetValue("refresh_token", out _) ||
            !Request.Cookies.TryGetValue("csrf_token", out var csrfToken) ||
            string.IsNullOrWhiteSpace(csrfToken))
        {
            throw new AppUnauthorizedException("Credenciales invalidas.");
        }

        Response.Headers.CacheControl = "no-store";
        return Ok(new { csrfToken });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDTO>> Refresh()
    {
        ValidateCsrfToken();
        var refreshToken = Request.Cookies["refresh_token"]
            ?? throw new AppUnauthorizedException("Credenciales invalidas.");
        var session = await _authService.RefreshAsync(refreshToken);
        Response.Cookies.Append("refresh_token", session.RefreshToken, CookieOptions());
        session.Response.CsrfToken = Request.Cookies["csrf_token"];
        return Ok(session.Response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        ValidateCsrfToken();
        if (Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
        {
            await _authService.LogoutAsync(refreshToken);
        }

        Response.Cookies.Delete("refresh_token", CookieOptions());
        Response.Cookies.Delete("csrf_token", CookieOptions(httpOnly: false));
        return NoContent();
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserDTO>> GetUserById(string id)
    {
        var user = await _authService.GetUserByIdAsync(id);
        return Ok(user);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyCollection<UserDTO>>> GetUsers()
    {
        var users = await _authService.GetUsersAsync();
        return Ok(users);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("users")]
    public async Task<ActionResult<UserDTO>> CreateUser([FromBody] CreateUserDTO createUserRequest)
    {
        var user = await _authService.CreateUserAsync(createUserRequest);
        _logger.LogInformation(
            "Usuario {TargetUserId} creado con rol {Role} por usuario {UserId}",
            user.Id,
            createUserRequest.Role,
            GetUserId());
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPut("users/{id}")]
    public async Task<ActionResult<UserDTO>> UpdateUser(string id, [FromBody] UpdateUserDTO updateUserRequest)
    {
        var user = await _authService.UpdateUserAsync(id, updateUserRequest);
        _logger.LogInformation("Usuario {TargetUserId} actualizado por usuario {UserId}", id, GetUserId());
        return Ok(user);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("users/{id}/unlock")]
    public async Task<ActionResult<UserDTO>> UnlockUser(string id)
    {
        var user = await _authService.UnlockUserAsync(id);
        _logger.LogInformation("Usuario {TargetUserId} desbloqueado por usuario {UserId}", id, GetUserId());
        return Ok(user);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("users/{id}/disable")]
    public async Task<ActionResult<UserDTO>> DisableUser(string id)
    {
        var user = await _authService.DisableUserAsync(id);
        _logger.LogInformation("Usuario {TargetUserId} deshabilitado por usuario {UserId}", id, GetUserId());
        return Ok(user);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("users/{id}/enable")]
    public async Task<ActionResult<UserDTO>> EnableUser(string id)
    {
        var user = await _authService.EnableUserAsync(id);
        _logger.LogInformation("Usuario {TargetUserId} habilitado por usuario {UserId}", id, GetUserId());
        return Ok(user);
    }

    private string GetUserId()
        => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

    private string SetSessionCookies(string refreshToken)
    {
        Response.Cookies.Append("refresh_token", refreshToken, CookieOptions());
        var csrfToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        Response.Cookies.Append("csrf_token", csrfToken, CookieOptions(httpOnly: false));
        return csrfToken;
    }

    private void ValidateCsrfToken()
    {
        if (!Request.Cookies.TryGetValue("csrf_token", out var csrfCookie) ||
            !Request.Headers.TryGetValue("X-CSRF-Token", out var csrfHeader) ||
            csrfHeader.Count != 1 || csrfCookie != csrfHeader[0])
        {
            throw new AppUnauthorizedException("Credenciales invalidas.");
        }
    }

    private CookieOptions CookieOptions(bool httpOnly = true)
    {
        var secure = Request.IsHttps || !_environment.IsDevelopment();
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(1)
        };
    }
}
