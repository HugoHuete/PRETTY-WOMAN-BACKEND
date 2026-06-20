namespace PrettyWoman.Application.DTOs.Auth;

public class CreateUserDTO
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Name { get; set; }
    public required string Lastname { get; set; }
    public required string Role { get; set; }
}
