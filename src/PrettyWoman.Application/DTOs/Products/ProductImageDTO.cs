namespace PrettyWoman.Application.DTOs.Products;

public class ProductImageDTO
{
    public int Id { get; set; }
    public required string ThumbnailUrl { get; set; }
    public required string WebUrl { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
