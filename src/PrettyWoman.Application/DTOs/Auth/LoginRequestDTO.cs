namespace PrettyWoman.Application.DTOs.Auth;

public class LoginRequestDTO
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
