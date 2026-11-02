using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSaleExchangeDTO
{
    public int? DeliveryAgencyId { get; set; }
    public string? DeliveryCode { get; set; }
    public string? DeliveryAddress { get; set; }
    public decimal ShippingChargedToClient { get; set; }
    public decimal ShippingPaidToAgency { get; set; }
    [MinLength(1)]
    public List<CreateExchangeReturnItemDTO> ReturnItems { get; set; } = [];
    [MinLength(1)]
    public List<CreateExchangeOutboundItemDTO> OutboundItems { get; set; } = [];
    public string? Comments { get; set; }
}
