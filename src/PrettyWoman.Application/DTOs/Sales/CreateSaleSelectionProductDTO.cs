using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSaleSelectionProductDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Producto es obligatorio.")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Quantity { get; set; }

    public string? Comments { get; set; }
}
