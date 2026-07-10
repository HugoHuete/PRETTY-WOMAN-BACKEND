using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.DeliveryAgencies;

public class DeliveryAgencyDTO
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nombre de la agencia de envío es obligatorio.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Teléfono de la agencia de envío es obligatorio.")]
    public required string PhoneNumber { get; set; }

    public bool Enabled { get; set; }
    public bool CanCollectCashOnDelivery { get; set; }
}
