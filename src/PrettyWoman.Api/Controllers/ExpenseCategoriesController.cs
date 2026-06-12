using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.ExpenseCategories;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ExpenseCategoriesController(IExpenseCategoryService expenseCategoryService) : ControllerBase
{
    private readonly IExpenseCategoryService _expenseCategoryService = expenseCategoryService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseCategoryDTO>>> GetAll()
    {
        var expenseCategories = await _expenseCategoryService.GetAllAsync();
        return Ok(expenseCategories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseCategoryDTO>> GetById(int id)
    {
        var expenseCategory = await _expenseCategoryService.GetByIdAsync(id);
        return Ok(expenseCategory);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateExpenseCategoryDTO createExpenseCategoryDTO)
    {
        var id = await _expenseCategoryService.CreateAsync(createExpenseCategoryDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseCategoryDTO updateExpenseCategoryDTO)
    {
        await _expenseCategoryService.UpdateAsync(id, updateExpenseCategoryDTO);
        return NoContent();
    }
}
