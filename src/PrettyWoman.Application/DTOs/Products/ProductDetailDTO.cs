namespace PrettyWoman.Application.DTOs.Products;

public class ProductDetailDTO
{
    public int Id { get; set; }
    public required string SupplierProductCode { get; set; }
    public int Code { get; set; }
    public required string Name { get; set; }
    public int SubcategoryId { get; set; }
    public string? SubcategoryName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public List<ProductVariantDTO> Products { get; set; } = [];
}
