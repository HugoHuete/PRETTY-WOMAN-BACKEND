using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.DeliveryAgencies;

public class CreateDeliveryAgencyDTO
{
    [Required(ErrorMessage = "Nombre de la agencia de envío es obligatorio.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Teléfono de la agencia de envío es obligatorio.")]
    public required string PhoneNumber { get; set; }

    public bool Enabled { get; set; } = true;
    public bool CanCollectCashOnDelivery { get; set; }
}
