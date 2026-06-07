namespace PrettyWoman.Domain.Entities;

public class ShippingCompany
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Url { get; set; }
}