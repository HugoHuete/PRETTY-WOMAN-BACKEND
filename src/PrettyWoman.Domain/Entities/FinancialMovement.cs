namespace PrettyWoman.Domain.Entities;

public class FinancialMovement
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MovementDirectionId { get; set; }
    public int FinancialMovementTypeId { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public int? OrderId { get; set; }
    public int? SalePaymentId { get; set; }
    public int? LoanId { get; set; }
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? Comments { get; set; }



    public MovementDirection? MovementDirection { get; set; }
    public FinancialMovementType? FinancialMovementType { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }
    public Order? Order { get; set; }
    public SalePayment? SalePayment { get; set; }
    public Loan? Loan { get; set; }

}