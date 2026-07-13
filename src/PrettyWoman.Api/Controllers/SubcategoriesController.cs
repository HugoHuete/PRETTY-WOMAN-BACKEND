using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Subcategories;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class SubcategoriesController(ISubcategoryService subcategoryService) : ControllerBase
{
    private readonly ISubcategoryService _subcategoryService = subcategoryService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubcategoryDTO>>> GetAll([FromQuery] int? categoryId)
    {
        var subcategories = await _subcategoryService.GetAllAsync(categoryId);
        return Ok(subcategories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubcategoryDTO>> GetById(int id)
    {
        var subcategory = await _subcategoryService.GetByIdAsync(id);
        return Ok(subcategory);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSubcategoryDTO createSubcategoryDTO)
    {
        var id = await _subcategoryService.CreateAsync(createSubcategoryDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubcategoryDTO updateSubcategoryDTO)
    {
        await _subcategoryService.UpdateAsync(id, updateSubcategoryDTO);
        return NoContent();
    }
}
