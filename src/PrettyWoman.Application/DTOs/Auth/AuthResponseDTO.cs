namespace PrettyWoman.Application.DTOs.Auth;

public class AuthResponseDTO
{
    public required string AccessToken { get; set; }
    public required DateTime ExpiresAtUtc { get; set; }
    public required UserDTO User { get; set; }
}
