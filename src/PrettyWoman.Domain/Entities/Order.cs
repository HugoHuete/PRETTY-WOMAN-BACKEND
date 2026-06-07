namespace PrettyWoman.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderStatusId { get; set; }
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountUsd { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal TotalShippingCost { get; set; }
    public string Comments { get; set; } = string.Empty;


    public OrderStatus? OrderStatus { get; set; }
    public Supplier? Supplier { get; set; }
    public List<OrderTrackingNumber> OrderTrackingNumbers = [];
}