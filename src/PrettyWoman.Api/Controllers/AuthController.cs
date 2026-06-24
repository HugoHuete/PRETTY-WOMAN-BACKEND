using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

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
        return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("users/{id}/unlock")]
    public async Task<ActionResult<UserDTO>> UnlockUser(string id)
    {
        var user = await _authService.UnlockUserAsync(id);
        return Ok(user);
    }
}
