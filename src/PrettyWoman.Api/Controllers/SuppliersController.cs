using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.Suppliers;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    private readonly ISupplierService _supplierService = supplierService;

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSupplierDTO createSupplierDTO)
    {
        var id = await _supplierService.CreateAsync(createSupplierDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id); ;
    }

    [HttpGet]
    public async Task<ActionResult<int>> GetById()
    {
        throw new NotImplementedException();
    }
}
