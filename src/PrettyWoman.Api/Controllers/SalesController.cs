using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.Common.Security;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AppPolicies.RequireEmployeeRole)]
public class SalesController(ISaleService saleService, ISaleExchangeService saleExchangeService, ISaleReturnService saleReturnService) : ControllerBase
{
    private readonly ISaleService _saleService = saleService;
    private readonly ISaleExchangeService _saleExchangeService = saleExchangeService;
    private readonly ISaleReturnService _saleReturnService = saleReturnService;

    /// <summary>Consulta ventas paginadas y permite filtrar por estado, pago, canal, clienta y fechas.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<SaleDTO>>> GetAll([FromQuery] SaleQueryDTO query)
    {
        var sales = await _saleService.GetAllAsync(query);
        return Ok(sales);
    }

    /// <summary>Obtiene el detalle de una venta cuando se necesita consultar o gestionar una venta específica.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleDTO>> GetById(int id)
    {
        var sale = await _saleService.GetByIdAsync(id);
        return Ok(sale);
    }

    /// <summary>Registra una nueva venta al confirmar una compra del cliente.</summary>
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSaleDTO createSaleDTO)
    {
        var id = await _saleService.CreateAsync(createSaleDTO);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Actualiza los datos generales de una venta mientras aún requiera correcciones.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> PatchHeader(int id, [FromBody] PatchSaleHeaderDTO patchSaleHeaderDTO)
    {
        await _saleService.PatchHeaderAsync(id, patchSaleHeaderDTO);
        return NoContent();
    }

    /// <summary>Reemplaza los productos de una venta para corregir su contenido antes de finalizarla.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPut("{id:int}/products")]
    public async Task<IActionResult> ReplaceProducts(int id, [FromBody] ReplaceSaleProductsDTO replaceSaleProductsDTO)
    {
        await _saleService.ReplaceProductsAsync(id, replaceSaleProductsDTO);
        return NoContent();
    }

    /// <summary>Agrega productos entregados en selección para registrar los que el cliente decidirá conservar.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/selection-holds")]
    public async Task<IActionResult> AddSelectionHolds(int id, [FromBody] List<CreateSaleSelectionProductDTO> selectionProducts)
    {
        await _saleService.AddSelectionHoldsAsync(id, selectionProducts);
        return NoContent();
    }

    /// <summary>Resuelve un producto en selección cuando el cliente confirma si lo conserva o lo devuelve.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/selection-holds/{holdId:int}/resolve")]
    public async Task<IActionResult> ResolveSelectionHold(int id, int holdId, [FromBody] ResolveSelectionHoldDTO resolution)
    {
        await _saleService.ResolveSelectionHoldAsync(id, holdId, resolution);
        return NoContent();
    }

    /// <summary>Marca como devuelto un producto en selección cuando regresa sin ser comprado.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/selection-holds/{holdId:int}/return")]
    public async Task<IActionResult> MarkSelectionHoldAsReturned(int id, int holdId)
    {
        await _saleService.MarkSelectionHoldAsReturnedAsync(id, holdId);
        return NoContent();
    }

    /// <summary>Consulta los intercambios asociados a una venta para revisar su historial y estado.</summary>
    [HttpGet("{id:int}/exchanges")]
    public async Task<ActionResult<IEnumerable<SaleExchangeDTO>>> GetExchanges(int id)
        => Ok(await _saleExchangeService.GetBySaleIdAsync(id));

    /// <summary>Registra un intercambio cuando el cliente devuelve productos y recibe otros a cambio.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/exchanges")]
    public async Task<ActionResult<int>> CreateExchange(int id, [FromBody] CreateSaleExchangeDTO exchange)
    {
        var exchangeId = await _saleExchangeService.CreateAsync(id, exchange);
        return CreatedAtAction(nameof(GetExchanges), new { id }, exchangeId);
    }

    /// <summary>Registra el intercambio físico: la agencia entrega las prendas nuevas y recibe las retornadas en el mismo acto.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/exchanges/{exchangeId:int}/handover")]
    public async Task<IActionResult> CompleteExchangeHandover(int id, int exchangeId)
    {
        await _saleExchangeService.CompleteHandoverAsync(id, exchangeId);
        return NoContent();
    }

    /// <summary>Confirma la recepción de un artículo devuelto para reincorporarlo al proceso de inventario.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/exchanges/{exchangeId:int}/return-items/{returnItemId:int}/received")]
    public async Task<IActionResult> MarkExchangeReturnReceived(int id, int exchangeId, int returnItemId)
    {
        await _saleExchangeService.MarkReturnReceivedAsync(id, exchangeId, returnItemId);
        return NoContent();
    }

    /// <summary>Cancela un intercambio cuando ya no debe continuar su procesamiento.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/exchanges/{exchangeId:int}/cancel")]
    public async Task<IActionResult> CancelExchange(int id, int exchangeId)
    {
        await _saleExchangeService.CancelAsync(id, exchangeId);
        return NoContent();
    }

    /// <summary>Consulta las devoluciones asociadas a una venta.</summary>
    [HttpGet("{id:int}/returns")]
    public async Task<ActionResult<IEnumerable<SaleReturnDTO>>> GetReturns(int id)
        => Ok(await _saleReturnService.GetBySaleIdAsync(id));

    /// <summary>Solicita una devolución parcial o total de productos ya entregados.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/returns")]
    public async Task<ActionResult<int>> CreateReturn(int id, [FromBody] CreateSaleReturnDTO request)
    {
        var returnId = await _saleReturnService.CreateAsync(id, request);
        return CreatedAtAction(nameof(GetReturns), new { id }, returnId);
    }

    /// <summary>Registra que la agencia recogió la devolución y ejecuta su reembolso.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/returns/{returnId:int}/pickup")]
    public async Task<IActionResult> RegisterReturnPickup(int id, int returnId, [FromBody] ProcessSaleReturnDTO request)
    {
        await _saleReturnService.RegisterAgencyPickupAsync(id, returnId, request);
        return NoContent();
    }

    /// <summary>Confirma la recepción física, reincorpora inventario o abre issue por daño.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/returns/{returnId:int}/receive")]
    public async Task<IActionResult> ReceiveReturn(int id, int returnId, [FromBody] ReceiveSaleReturnDTO request)
    {
        await _saleReturnService.ReceiveAsync(id, returnId, request);
        return NoContent();
    }

    /// <summary>Cancela una devolución aún no recogida ni recibida.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/returns/{returnId:int}/cancel")]
    public async Task<IActionResult> CancelReturn(int id, int returnId)
    {
        await _saleReturnService.CancelAsync(id, returnId);
        return NoContent();
    }

    /// <summary>Registra un abono, pago o ajuste de cobro aplicado a una venta.</summary>
    [HttpPost("{id:int}/payment-movements")]
    public async Task<ActionResult<int>> AddPaymentMovement(int id, [FromBody] CreateSalePaymentMovementDTO paymentMovement)
    {
        var paymentMovementId = await _saleService.AddPaymentMovementAsync(id, paymentMovement);
        return CreatedAtAction(nameof(GetById), new { id }, paymentMovementId);
    }

    /// <summary>Corrige los datos de un movimiento de pago cuando fue registrado con información incorrecta.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPatch("{id:int}/payment-movements/{paymentMovementId:int}")]
    public async Task<IActionResult> UpdatePaymentMovement(int id, int paymentMovementId, [FromBody] UpdateSalePaymentMovementDTO paymentMovement)
    {
        await _saleService.UpdatePaymentMovementAsync(id, paymentMovementId, paymentMovement);
        return NoContent();
    }

    /// <summary>Registra el reembolso de un movimiento de pago cuando se devuelve dinero al cliente.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/payment-movements/{paymentMovementId:int}/refunds")]
    public async Task<ActionResult<int>> RefundPaymentMovement(int id, int paymentMovementId, [FromBody] RefundSalePaymentMovementDTO refund)
    {
        var refundPaymentMovementId = await _saleService.RefundPaymentMovementAsync(id, paymentMovementId, refund);
        return CreatedAtAction(nameof(GetById), new { id }, refundPaymentMovementId);
    }

    /// <summary>Crea el envío de una venta cuando sus productos deben entregarse a domicilio o por agencia.</summary>
    [HttpPost("{id:int}/deliveries")]
    public async Task<ActionResult<int>> CreateDelivery(int id, [FromBody] CreateSaleDeliveryDTO delivery)
    {
        var deliveryId = await _saleService.CreateDeliveryAsync(id, delivery);
        return CreatedAtAction(nameof(GetById), new { id }, deliveryId);
    }

    /// <summary>Actualiza la información logística de un envío antes de que sea cerrado.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPatch("{id:int}/deliveries/{deliveryId:int}")]
    public async Task<IActionResult> UpdateDelivery(int id, int deliveryId, [FromBody] PatchSaleDeliveryDTO delivery)
    {
        await _saleService.UpdateDeliveryAsync(id, deliveryId, delivery);
        return NoContent();
    }

    /// <summary>Marca el envío como despachado cuando los productos salen hacia el cliente o la agencia.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/deliveries/{deliveryId:int}/send")]
    public async Task<IActionResult> MarkDeliveryAsSent(int id, int deliveryId)
    {
        await _saleService.MarkDeliveryAsSentAsync(id, deliveryId);
        return NoContent();
    }

    /// <summary>Indica que el envío fue entregado pero quedan productos en selección por resolver.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/deliveries/{deliveryId:int}/delivered-pending-selection")]
    public async Task<IActionResult> MarkDeliveryAsDeliveredPendingSelection(int id, int deliveryId)
    {
        await _saleService.MarkDeliveryAsDeliveredPendingSelectionAsync(id, deliveryId);
        return NoContent();
    }

    /// <summary>Finaliza un envío cuando la entrega y cualquier selección pendiente se completaron.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/deliveries/{deliveryId:int}/complete")]
    public async Task<IActionResult> MarkDeliveryAsCompleted(int id, int deliveryId)
    {
        await _saleService.MarkDeliveryAsCompletedAsync(id, deliveryId);
        return NoContent();
    }

    /// <summary>Marca el envío como fallido cuando no pudo ser entregado.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/deliveries/{deliveryId:int}/fail")]
    public async Task<IActionResult> MarkDeliveryAsFailed(int id, int deliveryId)
    {
        await _saleService.MarkDeliveryAsFailedAsync(id, deliveryId);
        return NoContent();
    }

    /// <summary>Cancela un envío cuando deja de ser necesario o no debe continuar.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/deliveries/{deliveryId:int}/cancel")]
    public async Task<IActionResult> CancelDelivery(int id, int deliveryId)
    {
        await _saleService.CancelDeliveryAsync(id, deliveryId);
        return NoContent();
    }

    /// <summary>Cancela una venta cuando debe anularse por completo.</summary>
    [Authorize(Policy = AppPolicies.RequireAdminRole)]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _saleService.CancelAsync(id);
        return NoContent();
    }
}
