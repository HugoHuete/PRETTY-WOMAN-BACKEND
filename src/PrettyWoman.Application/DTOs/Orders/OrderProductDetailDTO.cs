namespace PrettyWoman.Application.DTOs.Orders;

public class OrderProductDetailDTO
{
    public int Id { get; set; }
    public required string SupplierProductCode { get; set; }
    public int Code { get; set; }
    public required string Name { get; set; }
    public int SubcategoryId { get; set; }
    public string? SubcategoryName { get; set; }
    public ICollection<OrderProductVariantDTO> Variants { get; set; } = [];
}
