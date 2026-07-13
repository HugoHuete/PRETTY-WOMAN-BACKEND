using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Categories;
using PrettyWoman.Application.DTOs.Subcategories;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class CategoriesController(
    ICategoryService categoryService,
    ISubcategoryService subcategoryService) : ControllerBase
{
    private readonly ICategoryService _categoryService = categoryService;
    private readonly ISubcategoryService _subcategoryService = subcategoryService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDTO>> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        return Ok(category);
    }

    [HttpGet("{id:int}/subcategories")]
    public async Task<ActionResult<IEnumerable<SubcategoryDTO>>> GetSubcategories(int id)
    {
        var subcategories = await _subcategoryService.GetAllAsync(id);
        return Ok(subcategories);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateCategoryDTO createCategoryDTO)
    {
        var id = await _categoryService.CreateAsync(createCategoryDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDTO updateCategoryDTO)
    {
        await _categoryService.UpdateAsync(id, updateCategoryDTO);
        return NoContent();
    }
}
