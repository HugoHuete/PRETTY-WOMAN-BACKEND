using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.PaymentTerminals;

public class CreatePaymentTerminalDTO
{
    [Required]
    public required string Name { get; set; }

    [Range(0, 1, ErrorMessage = "Payment terminal comission percentage must be between 0 and 100.")]
    public decimal ComissionPercentage { get; set; }

    public bool Enabled { get; set; } = true;
}
