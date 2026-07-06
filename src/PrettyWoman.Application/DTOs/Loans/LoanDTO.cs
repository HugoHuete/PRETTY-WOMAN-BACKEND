namespace PrettyWoman.Application.DTOs.Loans;

public class LoanDTO
{
    public int Id { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LoanOwnerId { get; set; }
    public string? LoanOwnerName { get; set; }
    public decimal InitialAmount { get; set; }
    public decimal InitialAmountUsd { get; set; }
    public decimal Balance { get; set; }
    public decimal InterestPaidAmount { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Comments { get; set; }
    public decimal ExchangeRate { get; set; }
    public bool IsActive { get; set; }
    public List<LoanPaymentDTO> Payments { get; set; } = [];
}
