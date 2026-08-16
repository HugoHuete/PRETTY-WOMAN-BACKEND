using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Products;

public class UpdateProductPriceDTO
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El precio de venta debe ser mayor que cero.")]
    public decimal SalePrice { get; set; }
}
