using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Discounts;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AppPolicies.RequireAdminRole)]
public class DiscountCampaignsController(IDiscountCampaignService discountCampaignService) : ControllerBase
{
    private readonly IDiscountCampaignService _discountCampaignService = discountCampaignService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<DiscountCampaignSummaryDTO>>> GetAll(
        [FromQuery] DiscountCampaignQueryDTO query)
    {
        var discountCampaigns = await _discountCampaignService.GetAllAsync(query);
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

    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _discountCampaignService.CancelAsync(id);
        return NoContent();
    }

    [HttpPatch("{id:int}/reactivate")]
    public async Task<IActionResult> Reactivate(int id)
    {
        await _discountCampaignService.ReactivateAsync(id);
        return NoContent();
    }
}
