namespace PrettyWoman.Application.DTOs.Finances;

public class FinancialMovementQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? FinancialMovementTypeId { get; set; }
    public int? MovementDirectionId { get; set; }
}
