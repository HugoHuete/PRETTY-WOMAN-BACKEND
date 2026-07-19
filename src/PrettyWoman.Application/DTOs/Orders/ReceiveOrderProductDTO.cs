using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class ReceiveOrderProductDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Producto es obligatorio.")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad recibida debe ser mayor que cero.")]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335", ErrorMessage = "El peso estimado debe ser mayor que cero.")]
    public decimal Weight { get; set; } = 1;

    public bool IsSurplus { get; set; }

    public string? Comments { get; set; }
}
