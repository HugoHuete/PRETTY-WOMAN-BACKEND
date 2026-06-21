using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderStatusId { get; set; } = (int) OrderStatusCode.Pending;
    public int SupplierId { get; set; }
    public int PurchaseCurrencyId { get; set; } = (int)PurchaseCurrencyOption.Usd;
    public decimal AmountUsd { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal MerchandiseTotalNio { get; set; }
    public decimal ReceivedAmountNio { get; set; }
    public decimal SupplierShippingCostUsd { get; set; }
    public decimal WarehouseShippingCostUsd { get; set; }
    public decimal TotalCostNio { get; set; }
    public string? Comments { get; set; }


    public OrderStatus? OrderStatus { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<OrderTrackingNumber> OrderTrackingNumbers { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}