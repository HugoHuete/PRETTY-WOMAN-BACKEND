using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class UpdateOrderReceiptProductDTO
{
    [Range(1, int.MaxValue)]
    public int ProductReceiptDetailId { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Weight { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El precio de venta debe ser mayor que cero.")]
    public decimal? SalePrice { get; set; }
}
