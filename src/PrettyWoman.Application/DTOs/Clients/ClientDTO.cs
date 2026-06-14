using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Clients;

public class ClientDTO
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nombre del cliente es obligatorio.")]
    public required string Name { get; set; }

    public string? PhoneNumber { get; set; }
    public string? InstagramUser { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsFriend { get; set; }
    public string? BlockedReason { get; set; }
    public string? Comments { get; set; }
}
