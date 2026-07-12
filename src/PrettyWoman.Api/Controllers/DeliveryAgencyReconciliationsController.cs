using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DeliveryAgencyReconciliationsController(IDeliveryAgencyReconciliationService reconciliationService) : ControllerBase
{
    private readonly IDeliveryAgencyReconciliationService _reconciliationService = reconciliationService;

    [HttpGet("pending-deliveries")]
    public async Task<ActionResult<IEnumerable<PendingReconciliationDeliveryDTO>>> GetPendingDeliveries([FromQuery] int? deliveryAgencyId)
    {
        var deliveries = await _reconciliationService.GetPendingDeliveriesAsync(deliveryAgencyId);
        return Ok(deliveries);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateDeliveryAgencyReconciliationDTO reconciliation)
    {
        var id = await _reconciliationService.CreateAsync(reconciliation);
        return Ok(id);
    }
}
