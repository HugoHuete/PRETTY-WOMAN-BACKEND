namespace PrettyWoman.Domain.Entities;

public class ProductVariant
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int SizeId { get; set; }
    public string? Variant { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int UnavailableQuantity { get; set; }
    public decimal UnitCostUsd { get; set; }
    public decimal MerchandiseTotalCostNio { get; set; }
    public decimal AllocatedShippingCostNio { get; set; }
    public decimal TotalCostNio { get; set; }
    public decimal UnitCostNio { get; set; }
    public decimal SalePrice { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
    public Size? Size { get; set; }
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
    public ICollection<ProductHold> ProductHolds { get; set; } = [];
    public ICollection<ProductInventoryIssue> ProductInventoryIssues { get; set; } = [];
    public ICollection<ProductReceiptDetail> ProductReceiptDetails { get; set; } = [];
    public ICollection<InventoryAdjustmentItem> InventoryAdjustmentItems { get; set; } = [];
    public ICollection<PurchaseShortage> PurchaseShortages { get; set; } = [];
}
