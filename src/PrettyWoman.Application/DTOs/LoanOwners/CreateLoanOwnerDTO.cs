using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.LoanOwners;

public class CreateLoanOwnerDTO
{
    [Required(ErrorMessage = "Nombre del responsable del préstamo es obligatorio.")]
    public required string Name { get; set; }

    public bool Enabled { get; set; } = true;
}
