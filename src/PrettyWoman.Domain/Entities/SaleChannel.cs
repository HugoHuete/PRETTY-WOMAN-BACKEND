namespace PrettyWoman.Domain.Entities;

public class SaleChannel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool Enabled { get; set; } = true;
}