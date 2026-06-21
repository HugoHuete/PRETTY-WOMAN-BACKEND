namespace PrettyWoman.Application.DTOs.Orders;

public class OrderProductVariantDTO
{
    public int Id { get; set; }
    public int SizeId { get; set; }
    public string? SizeName { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public decimal UnitCostUsd { get; set; }
    public decimal MerchandiseTotalCostNio { get; set; }
    public decimal AllocatedShippingCostNio { get; set; }
    public decimal TotalCostNio { get; set; }
    public decimal UnitCostNio { get; set; }
    public decimal SalePrice { get; set; }
}
