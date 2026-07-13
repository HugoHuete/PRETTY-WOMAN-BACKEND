using PrettyWoman.Application.DTOs.Dashboard;

namespace PrettyWoman.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDTO> GetSummaryAsync(
        DashboardSummaryQueryDTO query,
        bool includeFinancialSummary,
        CancellationToken cancellationToken = default);
}
