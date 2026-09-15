using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/tracking-numbers")]
[Authorize(Policy = AppPolicies.RequireAdminRole)]
public class TrackingNumbersController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<OrderTrackingNumberDTO>>> GetAll(
        [FromQuery] OrderTrackingNumberQueryDTO query)
    {
        var trackingNumbers = await _orderService.GetAllTrackingNumbersAsync(query);
        return Ok(trackingNumbers);
    }
}
