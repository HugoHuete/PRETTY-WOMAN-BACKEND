namespace PrettyWoman.Application.DTOs.Finances;

public class MonthlyFinancialSummaryDTO
{
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
    public List<MonthlyFinancialSummaryYearDTO> Years { get; set; } = [];
    public FinancialSummaryTotalsDTO GrandTotal { get; set; } = new();
}
