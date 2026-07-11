namespace PrettyWoman.Domain.Entities;

public class FinancialMovement : IAuditableEntity
{
    public int Id { get; set; }
    public required string Description { get; set; }
    public DateTime MovementDate { get; set; }
    public int MovementDirectionId { get; set; }
    public int FinancialMovementTypeId { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public int? OrderId { get; set; }
    public int? ProductReceiptId { get; set; }
    public int? SalePaymentMovementId { get; set; }
    public int? LoanId { get; set; }
    public int? LoanPaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public MovementDirection? MovementDirection { get; set; }
    public FinancialMovementType? FinancialMovementType { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }
    public Order? Order { get; set; }
    public ProductReceipt? ProductReceipt { get; set; }
    public SalePaymentMovement? SalePaymentMovement { get; set; }
    public Loan? Loan { get; set; }
    public LoanPayment? LoanPayment { get; set; }
}

