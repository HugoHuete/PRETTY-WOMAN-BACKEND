namespace PrettyWoman.Domain.Entities;

public class Supplier
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Url { get; set; }

    public bool IsNational { get; set; } = true;
    public bool Enabled { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = [];
}