using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.LoanOwners;

public class CreateLoanOwnerDTO
{
    [Required]
    public required string Name { get; set; }

    public bool Enabled { get; set; } = true;
}
