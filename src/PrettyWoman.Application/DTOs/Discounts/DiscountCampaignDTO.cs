namespace PrettyWoman.Application.DTOs.Discounts;

public class DiscountCampaignDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool Enabled { get; set; }
    public List<DiscountCampaignProductDTO> Products { get; set; } = [];
}
