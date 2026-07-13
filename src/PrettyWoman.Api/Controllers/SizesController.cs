using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Sizes;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class SizesController(ISizeService sizeService) : ControllerBase
{
    private readonly ISizeService _sizeService = sizeService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SizeDTO>>> GetAll()
    {
        var sizes = await _sizeService.GetAllAsync();
        return Ok(sizes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SizeDTO>> GetById(int id)
    {
        var size = await _sizeService.GetByIdAsync(id);
        return Ok(size);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSizeDTO createSizeDTO)
    {
        var id = await _sizeService.CreateAsync(createSizeDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSizeDTO updateSizeDTO)
    {
        await _sizeService.UpdateAsync(id, updateSizeDTO);
        return NoContent();
    }
}
