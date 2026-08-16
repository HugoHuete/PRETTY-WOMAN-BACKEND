namespace PrettyWoman.Application.DTOs.Orders;

public class OrderReceiptSummaryDTO
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal WarehouseShippingCostUsd { get; set; }
    public decimal WarehouseShippingCostNio { get; set; }
    public int ProductCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public int TrackingCount { get; set; }
}
