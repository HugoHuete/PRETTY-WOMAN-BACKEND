using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.Discounts;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DiscountCampaignsController(IDiscountCampaignService discountCampaignService) : ControllerBase
{
    private readonly IDiscountCampaignService _discountCampaignService = discountCampaignService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiscountCampaignDTO>>> GetAll([FromQuery] bool? enabled)
    {
        var discountCampaigns = await _discountCampaignService.GetAllAsync(enabled);
        return Ok(discountCampaigns);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DiscountCampaignDTO>> GetById(int id)
    {
        var discountCampaign = await _discountCampaignService.GetByIdAsync(id);
        return Ok(discountCampaign);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateDiscountCampaignDTO createDiscountCampaignDTO)
    {
        var id = await _discountCampaignService.CreateAsync(createDiscountCampaignDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDiscountCampaignDTO updateDiscountCampaignDTO)
    {
        await _discountCampaignService.UpdateAsync(id, updateDiscountCampaignDTO);
        return NoContent();
    }

    [HttpPatch("{id:int}/disable")]
    public async Task<IActionResult> Disable(int id)
    {
        await _discountCampaignService.DisableAsync(id);
        return NoContent();
    }
}
