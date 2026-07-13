using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.PaymentTerminals;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class PaymentTerminalsController(IPaymentTerminalService paymentTerminalService) : ControllerBase
{
    private readonly IPaymentTerminalService _paymentTerminalService = paymentTerminalService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentTerminalDTO>>> GetAll()
    {
        var paymentTerminals = await _paymentTerminalService.GetAllAsync();
        return Ok(paymentTerminals);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentTerminalDTO>> GetById(int id)
    {
        var paymentTerminal = await _paymentTerminalService.GetByIdAsync(id);
        return Ok(paymentTerminal);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreatePaymentTerminalDTO createPaymentTerminalDTO)
    {
        var id = await _paymentTerminalService.CreateAsync(createPaymentTerminalDTO);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentTerminalDTO updatePaymentTerminalDTO)
    {
        await _paymentTerminalService.UpdateAsync(id, updatePaymentTerminalDTO);
        return NoContent();
    }
}
