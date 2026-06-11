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
        try
        {
            var supplierId = await _supplierService.CreateAsync(createSupplierDTO);
            return Created($"/api/v1/suppliers/{supplierId}", supplierId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
