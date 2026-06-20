namespace PrettyWoman.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductDetailId { get; set; }
    public int SizeId { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public decimal UnitCostUsd { get; set; }
    public decimal MerchandiseTotalCostNio { get; set; } // Total de todas las cantidades
    public decimal AllocatedShippingCostNio { get; set; } // Total de todas las cantidades
    public decimal TotalCostNio { get; set; } // MerchandiseTotalCostNio + AllocatedShippingCostNio
    public decimal UnitCostNio { get; set; } // TotalCostNio / quantity
    public decimal SalePrice { get; set; }


    public Order? Order { get; set; }
    public ProductDetail? ProductDetail { get; set; }
    public Size? Size { get; set; }
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
    public ICollection<ProductHold> ProductHolds { get; set; } = [];
    public ICollection<ProductReceiptDetail> ProductReceiptDetails { get; set; } = [];
    public ICollection<DiscountCampaignProduct> DiscountCampaignProducts { get; set; } = [];
}