namespace PrettyWoman.Domain.Entities;

public class DiscountCampaignProduct
{
    public int Id { get; set; }
    public int ProductDetailId { get; set; }
    public int DiscountCampaignId { get; set; }
    public int DiscountTypeId { get; set; }
    public decimal DiscountValue { get; set; }

    public DiscountType? DiscountType { get; set; }
    public DiscountCampaign? DiscountCampaign { get; set; }
    public ProductDetail? ProductDetail { get; set; }
}
