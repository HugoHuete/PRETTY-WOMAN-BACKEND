using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginRequestDTO loginRequest)
    {
        var response = await _authService.LoginAsync(loginRequest);
        return Ok(response);
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
        return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("users/{id}/unlock")]
    public async Task<ActionResult<UserDTO>> UnlockUser(string id)
    {
        var user = await _authService.UnlockUserAsync(id);
        _logger.LogInformation("Usuario {TargetUserId} desbloqueado por usuario {UserId}", id, GetUserId());
        return Ok(user);
    }

    private string GetUserId()
        => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
}
