using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Sales;

public class CreateExchangeOutboundItemDTO
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Range(1, 2)]
    public int ItemTypeId { get; set; }
    public string? Comments { get; set; }
}
