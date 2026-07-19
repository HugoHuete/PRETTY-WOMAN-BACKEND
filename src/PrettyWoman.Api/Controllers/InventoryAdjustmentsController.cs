using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.InventoryAdjustments;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/inventory-adjustments")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class InventoryAdjustmentsController(
    IInventoryAdjustmentService inventoryAdjustmentService,
    ILogger<InventoryAdjustmentsController> logger) : ControllerBase
{
    private readonly IInventoryAdjustmentService _inventoryAdjustmentService = inventoryAdjustmentService;
    private readonly ILogger<InventoryAdjustmentsController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<InventoryAdjustmentDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<InventoryAdjustmentDTO>>> GetAll([FromQuery] InventoryAdjustmentQueryDTO query)
    {
        var adjustments = await _inventoryAdjustmentService.GetAllAsync(query);
        return Ok(adjustments);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InventoryAdjustmentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryAdjustmentDTO>> GetById(int id)
    {
        var adjustment = await _inventoryAdjustmentService.GetByIdAsync(id);
        return Ok(adjustment);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create(CreateInventoryAdjustmentDTO request)
    {
        var id = await _inventoryAdjustmentService.CreateAsync(request);
        _logger.LogInformation("Ajuste de inventario creado {InventoryAdjustmentId} por usuario {UserId}", id, GetUserId());
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    private string GetUserId()
        => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
}
