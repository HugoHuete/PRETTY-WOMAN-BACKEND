namespace PrettyWoman.Domain.Entities;

public class LoanPayment
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? Comments { get; set; }

    public Loan? Loan { get; set; }
    public List<FinancialMovement> FinancialMovements { get; set; } = [];
}
