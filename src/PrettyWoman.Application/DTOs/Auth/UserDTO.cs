namespace PrettyWoman.Application.DTOs.Auth;

public class UserDTO
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Lastname { get; set; }
    public required string[] Roles { get; set; }
}
