namespace PrettyWoman.Application.DTOs.Finances;

public class CreateFinancialMovementDTO
{
    public required string Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int FinancialMovementTypeId { get; set; }
    public int? MovementDirectionId { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Comments { get; set; }
}
