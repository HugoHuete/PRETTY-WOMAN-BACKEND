using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Finances;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FinancesController(IFinancialService financialService) : ControllerBase
{
    private readonly IFinancialService _financialService = financialService;

    [HttpGet("current-balance")]
    public async Task<ActionResult<CurrentFinancialBalanceDTO>> GetCurrentBalance()
    {
        var balance = await _financialService.GetCurrentBalanceAsync();
        return Ok(balance);
    }

    [HttpGet("movements")]
    public async Task<ActionResult<PaginatedResult<FinancialMovementDTO>>> GetMovements([FromQuery] FinancialMovementQueryDTO query)
    {
        var movements = await _financialService.GetMovementsAsync(query);
        return Ok(movements);
    }
}
