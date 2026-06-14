using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class CreateOrderTrackingNumberDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Empresa de envío es obligatoria.")]
    public int ShippingCompanyId { get; set; }

    [Required(ErrorMessage = "Número de tracking es obligatorio.")]
    public required string TrackingNumber { get; set; }

    public DateTime? SupplierShipmentDate { get; set; }
    public DateTime? WarehouseDeliveryDate { get; set; }
    public int? ProductReceiptId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El peso debe ser mayor o igual a cero.")]
    public decimal Weight { get; set; } = 0;

    [Range(0, double.MaxValue, ErrorMessage = "El costo de envío debe ser mayor o igual a cero.")]
    public decimal ShippingCost { get; set; } = 0;
}
