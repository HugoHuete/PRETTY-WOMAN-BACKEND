using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSaleDeliveryDTO
{
    [Required(ErrorMessage = "El codigo del envio es obligatorio.")]
    public required string Code { get; set; }

    public int MunicipalityId { get; set; }
    public int DeliveryAgencyId { get; set; }
    public int? ClientId { get; set; }
    public decimal ShippingChargedToClient { get; set; }
    public string? Comments { get; set; }
}