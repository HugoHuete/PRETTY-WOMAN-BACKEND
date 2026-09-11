namespace PrettyWoman.Domain.Entities;

public class ProductPresentation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public int SortOrder { get; set; }

    public Product? Product { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = [];
    public ICollection<ProductImage> ProductImages { get; set; } = [];
}
