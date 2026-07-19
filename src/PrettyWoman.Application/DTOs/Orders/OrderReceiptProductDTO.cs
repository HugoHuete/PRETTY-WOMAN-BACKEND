namespace PrettyWoman.Application.DTOs.Orders;

public class OrderReceiptProductDTO
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public bool IsSurplus { get; set; }
    public decimal AllocatedWarehouseShippingCostNio { get; set; }
}
