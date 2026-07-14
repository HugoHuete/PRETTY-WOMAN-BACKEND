using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Products.InventoryIssues;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/product-inventory-issues")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class ProductInventoryIssuesController(
    IProductInventoryIssueService productInventoryIssueService,
    ILogger<ProductInventoryIssuesController> logger) : ControllerBase
{
    private readonly IProductInventoryIssueService _productInventoryIssueService = productInventoryIssueService;
    private readonly ILogger<ProductInventoryIssuesController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ProductInventoryIssueDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ProductInventoryIssueDTO>>> GetAll([FromQuery] ProductInventoryIssueQueryDTO query)
    {
        var issues = await _productInventoryIssueService.GetAllAsync(query);
        return Ok(issues);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductInventoryIssueDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductInventoryIssueDTO>> GetById(int id)
    {
        var issue = await _productInventoryIssueService.GetByIdAsync(id);
        return Ok(issue);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create(CreateProductInventoryIssueDTO createIssueDTO)
    {
        var id = await _productInventoryIssueService.CreateAsync(createIssueDTO);
        _logger.LogInformation("Ajuste de inventario creado {InventoryIssueId} por usuario {UserId}", id, GetUserId());
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPatch("{id:int}/resolution")]
    [ProducesResponseType(typeof(ProductInventoryIssueDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductInventoryIssueDTO>> Resolve(int id, ResolveProductInventoryIssueDTO resolveIssueDTO)
    {
        var issue = await _productInventoryIssueService.ResolveAsync(id, resolveIssueDTO);
        _logger.LogInformation("Ajuste de inventario resuelto {InventoryIssueId} por usuario {UserId}", id, GetUserId());
        return Ok(issue);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ProductInventoryIssueDTO>> Delete(int id)
    {
        var issue = await _productInventoryIssueService.DeleteAsync(id);
        _logger.LogInformation("Ajuste de inventario eliminado {InventoryIssueId} por usuario {UserId}", id, GetUserId());
        return Ok(issue);
    }

    private string GetUserId()
        => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
}
