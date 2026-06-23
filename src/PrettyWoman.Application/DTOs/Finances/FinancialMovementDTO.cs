namespace PrettyWoman.Application.DTOs.Finances;

public class FinancialMovementDTO
{
    public int Id { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MovementDirectionId { get; set; }
    public string? MovementDirectionName { get; set; }
    public int FinancialMovementTypeId { get; set; }
    public string? FinancialMovementTypeName { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public int? OrderId { get; set; }
    public int? SalePaymentId { get; set; }
    public int? LoanId { get; set; }
    public int? LoanPaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? Comments { get; set; }
}

