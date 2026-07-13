using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/orders/{orderId:int}/receipts")]
[Authorize(Policy = AppPolicies.RequireAdminRole)]
public class OrderReceiptsController(IOrderReceiptService orderReceiptService) : ControllerBase
{
    private readonly IOrderReceiptService _orderReceiptService = orderReceiptService;

    [HttpPost]
    public async Task<ActionResult<OrderReceiptDTO>> Receive(int orderId, [FromBody] ReceiveOrderDTO receiveOrderDTO)
    {
        var receipt = await _orderReceiptService.ReceiveAsync(orderId, receiveOrderDTO);
        return Ok(receipt);
    }
}
