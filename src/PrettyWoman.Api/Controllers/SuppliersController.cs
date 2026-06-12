using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.Suppliers;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    private readonly ISupplierService _supplierService = supplierService;

    [HttpGet]
    public async Task<ActionResult<SupplierDTO>> GetAll()
    {
        var suppliers = await _supplierService.GetAllAsync();
        return Ok(suppliers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierDTO>> GetById(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        return Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSupplierDTO createSupplierDTO)
    {
        var id = await _supplierService.CreateAsync(createSupplierDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id); ;
    }

    
}
