using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.DeliveryAgencies;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DeliveryAgenciesController(IDeliveryAgencyService deliveryAgencyService) : ControllerBase
{
    private readonly IDeliveryAgencyService _deliveryAgencyService = deliveryAgencyService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeliveryAgencyDTO>>> GetAll()
    {
        var deliveryAgencies = await _deliveryAgencyService.GetAllAsync();
        return Ok(deliveryAgencies);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeliveryAgencyDTO>> GetById(int id)
    {
        var deliveryAgency = await _deliveryAgencyService.GetByIdAsync(id);
        return Ok(deliveryAgency);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateDeliveryAgencyDTO createDeliveryAgencyDTO)
    {
        var id = await _deliveryAgencyService.CreateAsync(createDeliveryAgencyDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeliveryAgencyDTO updateDeliveryAgencyDTO)
    {
        await _deliveryAgencyService.UpdateAsync(id, updateDeliveryAgencyDTO);
        return NoContent();
    }
}
