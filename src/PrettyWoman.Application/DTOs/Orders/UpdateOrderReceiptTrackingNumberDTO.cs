using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class UpdateOrderReceiptTrackingNumberDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "El tracking es obligatorio.")]
    public int Id { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo de envío debe ser mayor o igual a cero.")]
    public decimal ShippingCostUsd { get; set; }
}
