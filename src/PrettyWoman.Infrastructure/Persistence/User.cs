using Microsoft.AspNetCore.Identity;

namespace PrettyWoman.Infrastructure.Persistence;

public class User : IdentityUser
{
    public required string Name { get; set; }
    public required string Lasname { get; set; }
}