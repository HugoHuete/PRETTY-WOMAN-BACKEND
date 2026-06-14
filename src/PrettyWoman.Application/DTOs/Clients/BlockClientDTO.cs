using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Clients;

public class BlockClientDTO
{
    [Required(ErrorMessage = "Motivo de bloqueo es obligatorio.")]
    public required string BlockedReason { get; set; }
}
