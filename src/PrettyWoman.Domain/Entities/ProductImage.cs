namespace PrettyWoman.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductDetailId { get; set; }
    public required string ImageUrl { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    public ProductDetail? ProductDetail { get; set; }
}