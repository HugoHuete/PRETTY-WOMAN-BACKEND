namespace PrettyWoman.Application.DTOs.Products;

public class ProductVariantDTO
{
    public int Id { get; set; }
    public int SizeId { get; set; }
    public string? SizeName { get; set; }
    public int? SizeGroupId { get; set; }
    public string? SizeGroupName { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int UnavailableQuantity { get; set; }
    public decimal SalePrice { get; set; }
}
