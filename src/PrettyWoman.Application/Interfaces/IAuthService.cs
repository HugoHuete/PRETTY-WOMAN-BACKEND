using PrettyWoman.Application.DTOs.Auth;

namespace PrettyWoman.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginRequest);
    Task<UserDTO> GetUserByIdAsync(string id);
    Task<IReadOnlyCollection<UserDTO>> GetUsersAsync();
    Task<UserDTO> CreateUserAsync(CreateUserDTO createUserRequest);
    Task<UserDTO> UpdateUserAsync(string id, UpdateUserDTO updateUserRequest);
    Task<UserDTO> UnlockUserAsync(string id);
    Task<UserDTO> DisableUserAsync(string id);
    Task<UserDTO> EnableUserAsync(string id);
}
