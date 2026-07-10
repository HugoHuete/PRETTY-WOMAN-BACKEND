using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Clients;

public class CreateClientDTO
{
    [Required(ErrorMessage = "Nombre del cliente es obligatorio.")]
    public required string Name { get; set; }

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    public string? PhoneNumber { get; set; }

    public string? InstagramUser { get; set; }
    public string? MessengerUser { get; set; }
    public string? Address { get; set; }
    public bool IsFriend { get; set; }
    public string? Comments { get; set; }
}
