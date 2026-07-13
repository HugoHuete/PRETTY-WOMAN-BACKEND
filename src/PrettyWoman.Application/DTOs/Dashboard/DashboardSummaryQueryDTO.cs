namespace PrettyWoman.Application.DTOs.Dashboard;

public class DashboardSummaryQueryDTO
{
    /// <summary>Fecha inicial inclusiva del período, en hora de Nicaragua.</summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>Fecha final inclusiva del período, en hora de Nicaragua.</summary>
    public DateOnly? ToDate { get; set; }
}
