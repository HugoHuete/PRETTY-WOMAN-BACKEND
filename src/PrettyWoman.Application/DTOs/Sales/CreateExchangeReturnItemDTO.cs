using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Sales;

public class CreateExchangeReturnItemDTO
{
    [Range(1, int.MaxValue)]
    public int OriginalSaleProductId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    // Credito acordado por cada unidad devuelta. Normalmente es el precio final pagado; puede ser menor por una excepcion acordada.
    [Range(0, double.MaxValue)]
    public decimal RecognizedUnitAmount { get; set; }
    public string? Comments { get; set; }
}
