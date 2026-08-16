namespace PrettyWoman.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public required string SupplierProductCode { get; set; }
    public int Code { get; set; }
    public required string Name { get; set; }
    public int SubcategoryId { get; set; }

    public Subcategory? Subcategory { get; set; }

    public ICollection<ProductVariant> ProductVariants { get; set; } = [];
    public ICollection<ProductImage> ProductImages { get; set; } = [];
    public ICollection<DiscountCampaignProduct> DiscountCampaignProducts { get; set; } = [];
}
