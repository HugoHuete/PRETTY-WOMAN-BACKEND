using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SalesController(ISaleService saleService) : ControllerBase
{
    private readonly ISaleService _saleService = saleService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleDTO>>> GetAll()
    {
        var sales = await _saleService.GetAllAsync();
        return Ok(sales);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleDTO>> GetById(int id)
    {
        var sale = await _saleService.GetByIdAsync(id);
        return Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSaleDTO createSaleDTO)
    {
        var id = await _saleService.CreateAsync(createSaleDTO);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSaleDTO updateSaleDTO)
    {
        await _saleService.UpdateAsync(id, updateSaleDTO);
        return NoContent();
    }
}
