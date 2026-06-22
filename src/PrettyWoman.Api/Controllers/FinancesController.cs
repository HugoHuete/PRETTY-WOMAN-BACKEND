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

    [HttpGet("movement-types")]
    public async Task<ActionResult<IEnumerable<FinancialMovementTypeDTO>>> GetMovementTypes()
    {
        var movementTypes = await _financialService.GetMovementTypesAsync();
        return Ok(movementTypes);
    }

    [HttpGet("movements")]
    public async Task<ActionResult<PaginatedResult<FinancialMovementDTO>>> GetMovements([FromQuery] FinancialMovementQueryDTO query)
    {
        var movements = await _financialService.GetMovementsAsync(query);
        return Ok(movements);
    }

    [HttpPost("movements")]
    public async Task<ActionResult<FinancialMovementDTO>> CreateMovement([FromBody] CreateFinancialMovementDTO createMovementDTO)
    {
        var movement = await _financialService.CreateManualMovementAsync(createMovementDTO);
        return Ok(movement);
    }

    [HttpPut("movements/{id:int}")]
    public async Task<ActionResult<FinancialMovementDTO>> UpdateMovement(int id, [FromBody] UpdateFinancialMovementDTO updateMovementDTO)
    {
        var movement = await _financialService.UpdateManualMovementAsync(id, updateMovementDTO);
        return Ok(movement);
    }

    [HttpDelete("movements/{id:int}")]
    public async Task<IActionResult> DeleteMovement(int id)
    {
        await _financialService.DeleteManualMovementAsync(id);
        return NoContent();
    }
}
