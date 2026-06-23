using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Loans;

public class UpdateLoanPaymentDTO
{
    public DateTime? CreatedAt { get; set; }

    public decimal Amount { get; set; }

    public decimal InterestAmount { get; set; }

    [MaxLength(300)]
    public string? Comments { get; set; }
}
