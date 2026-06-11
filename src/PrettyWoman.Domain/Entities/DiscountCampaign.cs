namespace PrettyWoman.Domain.Entities;

public class DiscountCampaign
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }
    public bool Enabled { get; set; } = true;

    public ICollection<DiscountCampaignProduct> DiscountCampaignProducts { get; set; } = [];
}