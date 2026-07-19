using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.InventoryCatalogs;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/inventory-catalogs")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class InventoryCatalogsController(IInventoryCatalogService inventoryCatalogService) : ControllerBase
{
    private readonly IInventoryCatalogService _inventoryCatalogService = inventoryCatalogService;

    [HttpGet("adjustment-reasons")]
    [ProducesResponseType(typeof(IEnumerable<InventoryCatalogItemDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryCatalogItemDTO>>> GetAdjustmentReasons()
    {
        var reasons = await _inventoryCatalogService.GetAdjustmentReasonsAsync();
        return Ok(reasons);
    }

    [HttpGet("adjustment-reason-suggestions")]
    [ProducesResponseType(typeof(IEnumerable<InventoryAdjustmentReasonSuggestionDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryAdjustmentReasonSuggestionDTO>>> GetAdjustmentReasonSuggestions()
    {
        var suggestions = await _inventoryCatalogService.GetAdjustmentReasonSuggestionsAsync();
        return Ok(suggestions);
    }

    [HttpGet("stock-buckets")]
    [ProducesResponseType(typeof(IEnumerable<InventoryCatalogItemDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryCatalogItemDTO>>> GetStockBuckets()
    {
        var buckets = await _inventoryCatalogService.GetStockBucketsAsync();
        return Ok(buckets);
    }
}
