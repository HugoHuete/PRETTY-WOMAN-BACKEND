namespace PrettyWoman.Domain.Entities;

public class Loan
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LoanOwnerId { get; set; }
    public decimal InitialAmount { get; set; }
    public decimal InitialAmountUsd { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Comments { get; set; }
    public decimal ExchangeRate { get; set; }

    public LoanOwner? LoanOwner { get; set; }
    public List<LoanPayment> LoanPayments { get; set; } = [];
    public List<FinancialMovement> FinancialMovements { get; set; } = [];
}
