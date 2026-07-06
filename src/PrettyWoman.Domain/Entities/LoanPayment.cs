namespace PrettyWoman.Domain.Entities;

public class LoanPayment : IAuditableEntity
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? Comments { get; set; }

    public DateTime PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public Loan? Loan { get; set; }
    public List<FinancialMovement> FinancialMovements { get; set; } = [];
}
