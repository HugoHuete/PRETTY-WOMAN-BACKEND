using PrettyWoman.Application.DTOs.Auth;

namespace PrettyWoman.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> LoginAsync(LoginRequestDTO loginRequest);
    Task<UserDTO> GetUserByIdAsync(string id);
    Task<UserDTO> CreateUserAsync(CreateUserDTO createUserRequest);
    Task<UserDTO> UnlockUserAsync(string id);
}
