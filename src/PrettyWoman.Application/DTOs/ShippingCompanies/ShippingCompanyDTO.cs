namespace PrettyWoman.Application.DTOs.ShippingCompanies;

public class ShippingCompanyDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Url { get; set; }
}
