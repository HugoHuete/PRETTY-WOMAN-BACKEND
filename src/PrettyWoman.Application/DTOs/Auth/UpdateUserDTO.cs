namespace PrettyWoman.Application.DTOs.Auth;

public class UpdateUserDTO
{
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Lastname { get; set; }
    public string? Password { get; set; }
}
