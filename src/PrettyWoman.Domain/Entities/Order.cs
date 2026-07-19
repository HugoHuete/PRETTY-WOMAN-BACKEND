using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

public class Order : IAuditableEntity
{
    public int Id { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int OrderStatusId { get; set; } = (int)OrderStatusCode.Pending;
    public int SupplierId { get; set; }
    public int PurchaseCurrencyId { get; set; } = (int)PurchaseCurrencyOption.Usd;
    public decimal AmountUsd { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal MerchandiseTotalNio { get; set; }
    public decimal ReceivedAmountNio { get; set; }
    public decimal SupplierShippingCostUsd { get; set; }
    public decimal WarehouseShippingCostUsd { get; set; }
    public decimal TotalCostNio { get; set; }
    public SupplierRefundResolutionOption? SupplierRefundResolution { get; set; }
    public DateTime? SupplierRefundDeclinedAt { get; set; }
    public string? SupplierRefundDeclineComments { get; set; }
    public string? Comments { get; set; }


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }


    public OrderStatus? OrderStatus { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<OrderTrackingNumber> OrderTrackingNumbers { get; set; } = [];
    public ICollection<ProductReceipt> ProductReceipts { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<PurchaseShortage> PurchaseShortages { get; set; } = [];
    public SupplierRefund? SupplierRefund { get; set; }
}
