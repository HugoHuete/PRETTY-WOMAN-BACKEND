namespace PrettyWoman.Domain.Entities;

public class ProductDetail
{
    public int Id { get; set; }
    public required string SupplierProductCode { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int SubcategoryId { get; set; }

    public Subcategory? Subcategory { get; set; }

    public ICollection<Product> Products { get; set; } = [];
    public ICollection<ProductImage> ProductImages { get; set; } = [];
}