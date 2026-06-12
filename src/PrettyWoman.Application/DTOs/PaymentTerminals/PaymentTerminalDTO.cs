using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.PaymentTerminals;

public class PaymentTerminalDTO
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public decimal ComissionPercentage { get; set; }

    public bool Enabled { get; set; }
}
