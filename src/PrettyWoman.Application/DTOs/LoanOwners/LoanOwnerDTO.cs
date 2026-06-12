using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.LoanOwners;

public class LoanOwnerDTO
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public bool Enabled { get; set; }
}
