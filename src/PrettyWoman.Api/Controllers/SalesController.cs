using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SalesController(ISaleService saleService) : ControllerBase
{
    private readonly ISaleService _saleService = saleService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleDTO>>> GetAll()
    {
        var sales = await _saleService.GetAllAsync();
        return Ok(sales);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleDTO>> GetById(int id)
    {
        var sale = await _saleService.GetByIdAsync(id);
        return Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSaleDTO createSaleDTO)
    {
        var id = await _saleService.CreateAsync(createSaleDTO);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> PatchHeader(int id, [FromBody] PatchSaleHeaderDTO patchSaleHeaderDTO)
    {
        await _saleService.PatchHeaderAsync(id, patchSaleHeaderDTO);
        return NoContent();
    }

    [HttpPut("{id:int}/products")]
    public async Task<IActionResult> ReplaceProducts(int id, [FromBody] ReplaceSaleProductsDTO replaceSaleProductsDTO)
    {
        await _saleService.ReplaceProductsAsync(id, replaceSaleProductsDTO);
        return NoContent();
    }

    [HttpPost("{id:int}/payment-movements")]
    public async Task<ActionResult<int>> AddPaymentMovement(int id, [FromBody] CreateSalePaymentMovementDTO paymentMovement)
    {
        var paymentMovementId = await _saleService.AddPaymentMovementAsync(id, paymentMovement);
        return CreatedAtAction(nameof(GetById), new { id }, paymentMovementId);
    }

    [HttpPatch("{id:int}/payment-movements/{paymentMovementId:int}")]
    public async Task<IActionResult> UpdatePaymentMovement(int id, int paymentMovementId, [FromBody] UpdateSalePaymentMovementDTO paymentMovement)
    {
        await _saleService.UpdatePaymentMovementAsync(id, paymentMovementId, paymentMovement);
        return NoContent();
    }

    [HttpPost("{id:int}/payment-movements/adjustments")]
    public async Task<IActionResult> AdjustPaymentMovements(int id, [FromBody] AdjustSalePaymentMovementsDTO adjustment)
    {
        await _saleService.AdjustPaymentMovementsAsync(id, adjustment);
        return NoContent();
    }

    [HttpPost("{id:int}/payment-movements/{paymentMovementId:int}/refunds")]
    public async Task<ActionResult<int>> RefundPaymentMovement(int id, int paymentMovementId, [FromBody] RefundSalePaymentMovementDTO refund)
    {
        var refundPaymentMovementId = await _saleService.RefundPaymentMovementAsync(id, paymentMovementId, refund);
        return CreatedAtAction(nameof(GetById), new { id }, refundPaymentMovementId);
    }

    [HttpPost("{id:int}/deliveries")]
    public async Task<ActionResult<int>> CreateDelivery(int id, [FromBody] CreateSaleDeliveryDTO delivery)
    {
        var deliveryId = await _saleService.CreateDeliveryAsync(id, delivery);
        return CreatedAtAction(nameof(GetById), new { id }, deliveryId);
    }

    [HttpPatch("{id:int}/deliveries/{deliveryId:int}")]
    public async Task<IActionResult> UpdateDelivery(int id, int deliveryId, [FromBody] PatchSaleDeliveryDTO delivery)
    {
        await _saleService.UpdateDeliveryAsync(id, deliveryId, delivery);
        return NoContent();
    }

    [HttpPost("{id:int}/deliveries/{deliveryId:int}/send")]
    public async Task<IActionResult> MarkDeliveryAsSent(int id, int deliveryId)
    {
        await _saleService.MarkDeliveryAsSentAsync(id, deliveryId);
        return NoContent();
    }

    [HttpPost("{id:int}/deliveries/{deliveryId:int}/complete")]
    public async Task<IActionResult> MarkDeliveryAsCompleted(int id, int deliveryId)
    {
        await _saleService.MarkDeliveryAsCompletedAsync(id, deliveryId);
        return NoContent();
    }

    [HttpPost("{id:int}/deliveries/{deliveryId:int}/fail")]
    public async Task<IActionResult> MarkDeliveryAsFailed(int id, int deliveryId)
    {
        await _saleService.MarkDeliveryAsFailedAsync(id, deliveryId);
        return NoContent();
    }

    [HttpPost("{id:int}/deliveries/{deliveryId:int}/cancel")]
    public async Task<IActionResult> CancelDelivery(int id, int deliveryId)
    {
        await _saleService.CancelDeliveryAsync(id, deliveryId);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _saleService.CancelAsync(id);
        return NoContent();
    }
}
