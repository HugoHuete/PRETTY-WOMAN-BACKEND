namespace PrettyWoman.Application.DTOs.Auth;

public class LoginRequestDTO
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
