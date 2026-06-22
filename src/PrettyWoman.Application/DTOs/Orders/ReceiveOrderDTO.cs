using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class ReceiveOrderDTO
{
    public DateTime? ReceivedDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo de envío de bodega a Nicaragua en dólares debe ser mayor o igual a cero.")]
    public decimal? WarehouseShippingCostUsd { get; set; }

    public string? Comments { get; set; }

    public ICollection<ReceiveOrderTrackingNumberDTO> TrackingNumbers { get; set; } = [];

    [MinLength(1, ErrorMessage = "Debe enviar al menos un producto recibido.")]
    public ICollection<ReceiveOrderProductDTO> Products { get; set; } = [];
}
