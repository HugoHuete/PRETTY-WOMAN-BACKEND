namespace PrettyWoman.Application.DTOs.Orders;

public class OrderReceiptDTO
{
    public int Id { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal WarehouseShippingCostUsd { get; set; }
    public decimal WarehouseShippingCostNio { get; set; }
    public int OrderStatusId { get; set; }
    public ICollection<OrderReceiptProductDTO> Products { get; set; } = [];
    public ICollection<int> TrackingNumberIds { get; set; } = [];
}
