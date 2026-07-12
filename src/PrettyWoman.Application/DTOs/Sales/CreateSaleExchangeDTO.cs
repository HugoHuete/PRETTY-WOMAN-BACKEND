using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSaleExchangeDTO
{
    [MinLength(1)]
    public List<CreateExchangeReturnItemDTO> ReturnItems { get; set; } = [];
    [MinLength(1)]
    public List<CreateExchangeOutboundItemDTO> OutboundItems { get; set; } = [];
    public string? Comments { get; set; }
}
