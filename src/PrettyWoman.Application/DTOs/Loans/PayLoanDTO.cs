using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Loans;

public class PayLoanDTO
{
    public DateTime? CreatedAt { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(300)]
    public string? Comments { get; set; }
}
