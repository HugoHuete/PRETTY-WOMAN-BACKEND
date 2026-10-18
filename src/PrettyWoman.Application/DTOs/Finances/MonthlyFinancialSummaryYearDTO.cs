namespace PrettyWoman.Application.DTOs.Finances;

public class MonthlyFinancialSummaryYearDTO : FinancialSummaryTotalsDTO
{
    public int Year { get; set; }
    public List<MonthlyFinancialSummaryMonthDTO> Months { get; set; } = [];
}
