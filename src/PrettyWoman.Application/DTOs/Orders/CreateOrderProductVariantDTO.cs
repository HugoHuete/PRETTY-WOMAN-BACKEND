using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class CreateOrderProductVariantDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Talla es obligatoria.")]
    public int SizeId { get; set; }

    public string? Color { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo unitario debe ser mayor o igual a cero.")]
    public decimal UnitCost { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a cero.")]
    public decimal SalePrice { get; set; }
}
