using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.PaymentTerminals;

public class PaymentTerminalDTO
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nombre de la terminal de pago es obligatorio.")]
    public required string Name { get; set; }

    public decimal ComissionPercentage { get; set; }

    public bool Enabled { get; set; }
}
