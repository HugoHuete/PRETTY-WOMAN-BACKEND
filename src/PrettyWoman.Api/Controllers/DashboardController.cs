using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Dashboard;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    private readonly IDashboardService _dashboardService = dashboardService;

    /// <summary>
    /// Obtiene el resumen operativo del período. Si no se indican fechas, consulta el día actual en hora de Nicaragua.
    /// El bloque financiero se incluye únicamente para el rol Admin.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardSummaryDTO>> GetSummary(
        [FromQuery] DashboardSummaryQueryDTO query,
        CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetSummaryAsync(
            query,
            User.IsInRole(AppRoles.Admin),
            cancellationToken);

        return Ok(summary);
    }
}
