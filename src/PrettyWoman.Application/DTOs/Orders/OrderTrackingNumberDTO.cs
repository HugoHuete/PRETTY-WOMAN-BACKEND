using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class OrderTrackingNumberDTO
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ShippingCompanyId { get; set; }

    [Required(ErrorMessage = "Número de tracking es obligatorio.")]
    public required string TrackingNumber { get; set; }

    public DateTime? SupplierShipmentDate { get; set; }
    public DateTime? WarehouseDeliveryDate { get; set; }
    public int? ProductReceiptId { get; set; }
    public decimal Weight { get; set; }
    public decimal ShippingCost { get; set; }
    public string? ShippingCompanyName { get; set; }
}
