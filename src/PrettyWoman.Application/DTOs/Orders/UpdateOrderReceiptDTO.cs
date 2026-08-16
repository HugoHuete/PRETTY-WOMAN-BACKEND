using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class UpdateOrderReceiptDTO
{
    [Range(0, double.MaxValue, ErrorMessage = "El costo de envío de bodega a Nicaragua en dólares debe ser mayor o igual a cero.")]
    public decimal? WarehouseShippingCostUsd { get; set; }

    public ICollection<UpdateOrderReceiptTrackingNumberDTO> TrackingNumbers { get; set; } = [];

    public ICollection<UpdateOrderReceiptProductDTO> ProductVariants { get; set; } = [];
}
