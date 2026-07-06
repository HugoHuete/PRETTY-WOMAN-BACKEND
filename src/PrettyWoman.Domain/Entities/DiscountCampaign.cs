namespace PrettyWoman.Domain.Entities;

public class DiscountCampaign : IAuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public ICollection<DiscountCampaignProduct> DiscountCampaignProducts { get; set; } = [];
}