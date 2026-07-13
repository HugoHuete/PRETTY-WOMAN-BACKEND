using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AppPolicies.RequireAdminRole)]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAll()
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDTO>> GetById(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        return Ok(order);
    }

    [HttpGet("{id:int}/tracking-numbers")]
    public async Task<ActionResult<IEnumerable<OrderTrackingNumberDTO>>> GetTrackingNumbers(int id)
    {
        var trackingNumbers = await _orderService.GetTrackingNumbersAsync(id);
        return Ok(trackingNumbers);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateOrderDTO createOrderDTO)
    {
        var id = await _orderService.CreateAsync(createOrderDTO);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDTO updateOrderDTO)
    {
        await _orderService.UpdateAsync(id, updateOrderDTO);
        return NoContent();
    }

    [HttpPost("{id:int}/tracking-numbers")]
    public async Task<ActionResult<IEnumerable<OrderTrackingNumberDTO>>> AddTrackingNumbers(
        int id,
        [FromBody] IEnumerable<CreateOrderTrackingNumberDTO> createTrackingDTOs)
    {
        var trackingNumbers = await _orderService.AddTrackingNumbersAsync(id, createTrackingDTOs);
        return Ok(trackingNumbers);
    }

    [HttpPut("{id:int}/tracking-numbers/{trackingId:int}")]
    public async Task<ActionResult<OrderTrackingNumberDTO>> UpdateTrackingNumber(
        int id,
        int trackingId,
        [FromBody] UpdateOrderTrackingNumberDTO updateTrackingDTO)
    {
        var trackingNumber = await _orderService.UpdateTrackingNumberAsync(id, trackingId, updateTrackingDTO);
        return Ok(trackingNumber);
    }

    [HttpDelete("{id:int}/tracking-numbers/{trackingId:int}")]
    public async Task<IActionResult> DeleteTrackingNumber(int id, int trackingId)
    {
        await _orderService.DeleteTrackingNumberAsync(id, trackingId);
        return NoContent();
    }
}
