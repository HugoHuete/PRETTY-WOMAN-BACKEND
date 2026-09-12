namespace PrettyWoman.Application.DTOs.Products;

public class ProductPresentationDTO
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int SortOrder { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public List<ProductVariantDTO> Sizes { get; set; } = [];
}
