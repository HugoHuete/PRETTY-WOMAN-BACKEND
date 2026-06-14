namespace PrettyWoman.Domain.Entities;

public class OrderTrackingNumber
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ShippingCompanyId { get; set; }
    public required string TrackingNumber { get; set; }
    public DateTime? SupplierShipmentDate { get; set; }
    public DateTime? WarehouseDeliveryDate { get; set; }
    public int? ProductReceiptId { get; set; }
    public decimal Weight { get; set; } = 0;
    public decimal ShippingCost { get; set; } = 0;

    // Foreign Keys
    public ShippingCompany? ShippingCompany { get; set; }
    public Order? Order { get; set; }
    public ProductReceipt? ProductReceipt { get; set; }
}