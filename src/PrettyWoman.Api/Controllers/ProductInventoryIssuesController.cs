using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products.InventoryIssues;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/product-inventory-issues")]
public class ProductInventoryIssuesController(IProductInventoryIssueService productInventoryIssueService) : ControllerBase
{
    private readonly IProductInventoryIssueService _productInventoryIssueService = productInventoryIssueService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProductInventoryIssueDTO>>> GetAll([FromQuery] ProductInventoryIssueQueryDTO query)
    {
        var issues = await _productInventoryIssueService.GetAllAsync(query);
        return Ok(issues);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductInventoryIssueDTO>> GetById(int id)
    {
        var issue = await _productInventoryIssueService.GetByIdAsync(id);
        return Ok(issue);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateProductInventoryIssueDTO createIssueDTO)
    {
        var id = await _productInventoryIssueService.CreateAsync(createIssueDTO);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPatch("{id:int}/resolution")]
    public async Task<ActionResult<ProductInventoryIssueDTO>> Resolve(int id, ResolveProductInventoryIssueDTO resolveIssueDTO)
    {
        var issue = await _productInventoryIssueService.ResolveAsync(id, resolveIssueDTO);
        return Ok(issue);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ProductInventoryIssueDTO>> Delete(int id)
    {
        var issue = await _productInventoryIssueService.DeleteAsync(id);
        return Ok(issue);
    }
}