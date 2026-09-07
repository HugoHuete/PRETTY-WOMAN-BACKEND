using PrettyWoman.Application.DTOs.Auth;

namespace PrettyWoman.Application.Interfaces;

public interface IAuthService
{
    Task<AuthSessionDTO> LoginAsync(LoginRequestDTO loginRequest);
    Task<AuthSessionDTO> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<UserDTO> GetUserByIdAsync(string id);
    Task<IReadOnlyCollection<UserDTO>> GetUsersAsync(string? user = null, string? role = null, bool? enabled = null);
    Task<UserDTO> CreateUserAsync(CreateUserDTO createUserRequest);
    Task<UserDTO> UpdateUserAsync(string id, UpdateUserDTO updateUserRequest);
    Task<UserDTO> UnlockUserAsync(string id);
    Task<UserDTO> DisableUserAsync(string id);
    Task<UserDTO> EnableUserAsync(string id);
}
