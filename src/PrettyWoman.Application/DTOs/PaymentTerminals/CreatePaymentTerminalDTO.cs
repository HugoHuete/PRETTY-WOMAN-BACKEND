using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.PaymentTerminals;

public class CreatePaymentTerminalDTO
{
    [Required(ErrorMessage = "Nombre de la terminal de pago es obligatorio.")]
    public required string Name { get; set; }

    [Range(0, 100, ErrorMessage = "El porcentaje de comisión de la terminal de pago debe estar entre 0 y 100.")]
    public decimal ComissionPercentage { get; set; }

    [Range(0, 100, ErrorMessage = "El porcentaje de impuesto de la terminal de pago debe estar entre 0 y 100.")]
    public decimal IncomeTaxPercentage { get; set; }

    public bool Enabled { get; set; } = true;
}
