namespace PrettyWoman.Application.DTOs.Auth;

public class AuthSessionDTO
{
    public required AuthResponseDTO Response { get; init; }
    public required string RefreshToken { get; init; }
}
