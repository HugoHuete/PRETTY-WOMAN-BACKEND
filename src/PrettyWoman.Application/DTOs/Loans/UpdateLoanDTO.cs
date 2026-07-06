using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Loans;

public class UpdateLoanDTO
{
    public DateTime? LoanDate { get; set; }

    public int LoanOwnerId { get; set; }

    public decimal InitialAmount { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }
}