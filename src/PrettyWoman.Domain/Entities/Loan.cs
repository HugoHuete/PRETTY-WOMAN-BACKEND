namespace PrettyWoman.Domain.Entities;

public class Loan
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LoanOwnerId { get; set; }
    public decimal InitialAmount { get; set; }
    public decimal InitialAmountUsd { get; set; }
    public decimal Balance { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Comments { get; set; }
    public int DollarExchangeRateId { get; set; }



    public bool IsActive => Balance > 0 && ClosedAt is null;

    public LoanOwner? LoanOwner { get; set; }
    public DollarExchangeRate? DollarExchangeRate { get; set; }
    public List<FinancialMovement> FinancialMovements { get; set; } = [];
}