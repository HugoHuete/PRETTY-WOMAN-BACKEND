using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Calculations;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class DeliveryAgencyReconciliationService(IApplicationDbContext context, ICurrentUserService currentUserService) : IDeliveryAgencyReconciliationService
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<IEnumerable<PendingReconciliationDeliveryDTO>> GetPendingDeliveriesAsync(int? deliveryAgencyId = null)
    {
        var query = _context.SaleDeliveries
            .AsNoTracking()
            .Include(item => item.DeliveryAgency)
            .Include(item => item.DeliveryStatus)
            .Include(item => item.Client)
            .Include(item => item.Sale)
            .Where(item =>
                !item.DeliveryAgencyReconciliationId.HasValue &&
                (item.DeliveryStatusId == (int)DeliveryStatusCode.Completed ||
                 item.DeliveryStatusId == (int)DeliveryStatusCode.Failed) &&
                !_context.ProductHolds.Any(hold =>
                    hold.SaleId == item.SaleId &&
                    hold.ProductHoldStatusId == (int)ProductHoldStatusOption.Active));

        if (deliveryAgencyId.HasValue)
        {
            query = query.Where(item => item.DeliveryAgencyId == deliveryAgencyId.Value);
        }

        var deliveries = await query
            .OrderBy(item => item.DeliveryAgency!.Name)
            .ThenBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync();

        return deliveries.Select(item => new PendingReconciliationDeliveryDTO
        {
            SaleDeliveryId = item.Id,
            Code = item.Code,
            CreatedAt = item.CreatedAt,
            DeliveryStatusId = item.DeliveryStatusId,
            DeliveryStatusName = item.DeliveryStatus?.Name,
            DeliveryAgencyId = item.DeliveryAgencyId,
            DeliveryAgencyName = item.DeliveryAgency?.Name,
            SaleId = item.SaleId,
            SaleTotal = item.Sale?.Total ?? 0,
            ClientId = item.ClientId,
            ClientName = item.Client?.Name,
            DeliveryAddress = item.DeliveryAddress,
            AmountToCollect = item.AmountToCollect,
            ShippingChargedToClient = item.ShippingChargedToClient
        }).ToList();
    }

    public async Task<int> CreateAsync(CreateDeliveryAgencyReconciliationDTO request)
    {
        NormalizeAndValidateRequest(request);
        EnsureDistinctDeliveryIds(request.Deliveries);
        EnsureDistinctReturnIds(request.Returns);

        var agency = await _context.DeliveryAgencies
            .FirstOrDefaultAsync(item => item.Id == request.DeliveryAgencyId)
            ?? throw new AppNotFoundException($"La agencia de envio con id {request.DeliveryAgencyId} no existe.");

        var deliveryIds = request.Deliveries.Select(item => item.SaleDeliveryId).ToList();
        var returnIds = request.Returns.Select(item => item.SaleReturnId).ToList();
        // Se cargan los pagos existentes para calcular el saldo real al momento de conciliar,
        // no el monto que tenia el envio cuando fue creado.
        var deliveries = await _context.SaleDeliveries
            .Include(item => item.Sale)
                .ThenInclude(sale => sale!.PaymentMovements)
            .Include(item => item.Sale)
                .ThenInclude(sale => sale!.ProductHolds)
            .Where(item => deliveryIds.Contains(item.Id))
            .ToListAsync();

        if (deliveries.Count != deliveryIds.Count)
            throw new AppNotFoundException("Uno o mas envios indicados no existen.");

        var returns = await _context.SaleReturns.Where(item => returnIds.Contains(item.Id)).ToListAsync();
        if (returns.Count != returnIds.Count)
            throw new AppNotFoundException("Una o mas devoluciones indicadas no existen.");
        foreach (var saleReturn in returns) ValidateReturnForReconciliation(saleReturn, agency.Id);

        var reconciliation = new DeliveryAgencyReconciliation
        {
            DeliveryAgencyId = agency.Id,
            ReconciliationDate = request.ReconciliationDate ?? DateTime.UtcNow,
            SettlementExchangeRate = request.SettlementExchangeRate,
            AmountReceivedNio = Math.Round(request.Deliveries.Sum(item => item.AmountCollectedNio), 2),
            AmountReceivedUsd = Math.Round(request.Deliveries.Sum(item => item.AmountCollectedUsd), 2),
            AmountPaidToAgencyNio = Math.Round(request.Deliveries.Sum(item => item.ChangeGivenNio + item.ShippingPaidToAgency) + returns.Sum(item => item.ReturnShippingPaidToAgency), 2),
            Comments = request.Comments
        };

        var deliveryRequests = request.Deliveries.ToDictionary(item => item.SaleDeliveryId);
        var salesWithNewPayments = new Dictionary<int, Sale>();
        // Los cobros de agencia se registran como pagos de la venta, pero el efectivo solo
        // entra o sale de caja mediante los movimientos financieros de la conciliacion.
        foreach (var delivery in deliveries)
        {
            var deliveryRequest = deliveryRequests[delivery.Id];
            var collectedAmount = CalculateCollectedAmount(deliveryRequest);
            ValidateDeliveryForReconciliation(delivery, deliveryRequest, agency.Id, collectedAmount);
            ApplyDeliveryCollection(delivery, deliveryRequest, reconciliation);

            if (delivery.DeliveryStatusId == (int)DeliveryStatusCode.Completed && collectedAmount > 0)
            {
                var payment = CreateAgencyCollectionPayment(delivery.Sale!, delivery, collectedAmount, reconciliation, ResolveUserId());
                delivery.Sale!.PaymentMovements.Add(payment);
                salesWithNewPayments[delivery.SaleId] = delivery.Sale;
            }
        }

        foreach (var sale in salesWithNewPayments.Values)
        {
            var productPaymentTotal = SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements);
            SalePaymentMovementRules.EnsureAllowedProductTotal(
                sale.SaleChannelId,
                sale.Total,
                productPaymentTotal,
                allowOverpayment: sale.SalePaymentStatusId == (int)SalePaymentStatusOption.RefundPending);
            sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(sale.Total, productPaymentTotal);
        }

        foreach (var delivery in deliveries)
        {
            delivery.DeliveryAgencyReconciliation = reconciliation;
        }
        foreach (var saleReturn in returns)
        {
            saleReturn.DeliveryAgencyReconciliation = reconciliation;
        }

        await _context.DeliveryAgencyReconciliations.AddAsync(reconciliation);
        // Una liquidacion puede tener remesa, pago a la agencia, ambos, o ninguno.
        await AddFinancialMovementsAsync(reconciliation, agency.Name);
        await _context.SaveChangesAsync();

        return reconciliation.Id;
    }

    private async Task AddFinancialMovementsAsync(DeliveryAgencyReconciliation reconciliation, string agencyName)
    {
        // La tasa de liquidacion es historica y puede diferir de la tasa aplicada al cliente.
        var amountReceivedNio = Math.Round(
            reconciliation.AmountReceivedNio + reconciliation.AmountReceivedUsd * reconciliation.SettlementExchangeRate,
            2);
        var amountPaidNio = Math.Round(
            reconciliation.AmountPaidToAgencyNio,
            2);

        if (amountReceivedNio > 0)
        {
            await _context.FinancialMovements.AddAsync(new FinancialMovement
            {
                Description = $"Remesa recibida de {agencyName}",
                MovementDate = reconciliation.ReconciliationDate,
                MovementDirectionId = (int)MovementDirectionOptions.In,
                FinancialMovementTypeId = (int)FinancialMovementTypeOption.DeliveryAgencyReconciliation,
                DeliveryAgencyReconciliation = reconciliation,
                Amount = amountReceivedNio,
                ExchangeRate = reconciliation.SettlementExchangeRate,
                Comments = reconciliation.Comments
            });
        }

        if (amountPaidNio > 0)
        {
            await _context.FinancialMovements.AddAsync(new FinancialMovement
            {
                Description = $"Liquidacion pagada a {agencyName}",
                MovementDate = reconciliation.ReconciliationDate,
                MovementDirectionId = (int)MovementDirectionOptions.Out,
                FinancialMovementTypeId = (int)FinancialMovementTypeOption.DeliveryAgencyReconciliation,
                DeliveryAgencyReconciliation = reconciliation,
                Amount = amountPaidNio,
                ExchangeRate = reconciliation.SettlementExchangeRate,
                Comments = reconciliation.Comments
            });
        }
    }

    private static SalePaymentMovement CreateAgencyCollectionPayment(
        Sale sale,
        SaleDelivery delivery,
        decimal collectedAmount,
        DeliveryAgencyReconciliation reconciliation,
        string userId)
    {
        var outstandingProductAmount = Math.Max(0, sale.Total - SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements));
        var outstandingShippingAmount = Math.Max(0, delivery.ShippingChargedToClient - SalePaymentMovementRules.CalculateShippingTotal(sale.PaymentMovements, delivery.Id));
        var outstandingTotal = outstandingProductAmount + outstandingShippingAmount;
        if (collectedAmount > outstandingTotal)
            throw new AppBadRequestException("El monto recolectado no puede exceder el saldo pendiente de la venta y su envio.");

        // El cobro contra entrega cubre primero productos y luego el envio pendiente.
        var productAmount = Math.Min(collectedAmount, outstandingProductAmount);
        var shippingAmount = collectedAmount - productAmount;

        return new SalePaymentMovement
        {
            MovementDate = reconciliation.ReconciliationDate,
            SaleId = sale.Id,
            MovementDirectionId = (int)MovementDirectionOptions.In,
            PaymentMethodId = (int)PaymentMethodOption.Transfer,
            GrossAmount = collectedAmount,
            ProductAmount = productAmount,
            ShippingAmount = shippingAmount,
            SaleDeliveryId = shippingAmount > 0 ? delivery.Id : null,
            DeliveryAgencyReconciliation = reconciliation,
            NetReceivedAmount = collectedAmount,
            UserId = userId
        };
    }

    private static void ApplyDeliveryCollection(
        SaleDelivery delivery,
        ReconcileSaleDeliveryDTO request,
        DeliveryAgencyReconciliation reconciliation)
    {
        delivery.AmountCollectedNio = request.AmountCollectedNio;
        delivery.AmountCollectedUsd = request.AmountCollectedUsd;
        delivery.ChangeGivenNio = request.ChangeGivenNio;
        delivery.CollectionExchangeRate = request.CollectionExchangeRate;
        delivery.ShippingPaidToAgency = request.ShippingPaidToAgency;
        delivery.DeliveryAgencyReconciliation = reconciliation;
    }

    private static void ValidateDeliveryForReconciliation(
        SaleDelivery delivery,
        ReconcileSaleDeliveryDTO request,
        int deliveryAgencyId,
        decimal collectedAmount)
    {
        if (delivery.DeliveryAgencyId != deliveryAgencyId)
            throw new AppBadRequestException("Todos los envios de una conciliacion deben pertenecer a la misma agencia.");
        if (delivery.DeliveryAgencyReconciliationId.HasValue)
            throw new AppBadRequestException("Un envio no puede incluirse en mas de una conciliacion.");
        if (delivery.DeliveryStatusId is not ((int)DeliveryStatusCode.Completed or (int)DeliveryStatusCode.Failed))
            throw new AppBadRequestException("Solo se pueden conciliar envios completados o fallidos.");
        if (delivery.Sale!.ProductHolds.Any(hold => hold.ProductHoldStatusId == (int)ProductHoldStatusOption.Active))
            throw new AppBadRequestException("No se puede conciliar un envio con prendas enviadas para seleccion sin resolver.");

        ValidateDeliveryAmounts(request);
        if (delivery.DeliveryStatusId == (int)DeliveryStatusCode.Failed && collectedAmount != 0)
            throw new AppBadRequestException("Un envio fallido no puede registrar cobros de la clienta.");
        if (collectedAmount > delivery.AmountToCollect)
            throw new AppBadRequestException("El monto recolectado no puede exceder el monto indicado para cobrar en el envio.");
    }

    // El efectivo neto recaudado excluye el vuelto entregado en cordobas.
    private static decimal CalculateCollectedAmount(ReconcileSaleDeliveryDTO request)
        => Math.Round(
            request.AmountCollectedNio + request.AmountCollectedUsd * (request.CollectionExchangeRate ?? 0) - request.ChangeGivenNio,
            2);

    private static void NormalizeAndValidateRequest(CreateDeliveryAgencyReconciliationDTO request)
    {
        request.Comments = request.Comments.NormalizeOptional();
        request.Deliveries ??= [];
        request.Returns ??= [];
        if (request.Deliveries.Count == 0 && request.Returns.Count == 0)
            throw new AppBadRequestException("Debe indicar al menos un envío o devolución para conciliar.");
        if (request.SettlementExchangeRate <= 0)
            throw new AppBadRequestException("La tasa de cambio usada para la liquidacion debe ser mayor que cero.");
    }

    private static void EnsureDistinctDeliveryIds(IEnumerable<ReconcileSaleDeliveryDTO> deliveries)
    {
        if (deliveries.Select(item => item.SaleDeliveryId).Distinct().Count() != deliveries.Count())
            throw new AppBadRequestException("No se puede incluir el mismo envio mas de una vez en una conciliacion.");
    }

    private static void EnsureDistinctReturnIds(IEnumerable<ReconcileSaleReturnDTO> returns)
    {
        if (returns.Select(item => item.SaleReturnId).Distinct().Count() != returns.Count())
            throw new AppBadRequestException("No se puede incluir la misma devolución más de una vez en una conciliación.");
    }

    private static void ValidateReturnForReconciliation(SaleReturn saleReturn, int deliveryAgencyId)
    {
        if (saleReturn.DeliveryAgencyId != deliveryAgencyId)
            throw new AppBadRequestException("Todas las devoluciones de una conciliación deben pertenecer a la misma agencia.");
        if (saleReturn.DeliveryAgencyReconciliationId.HasValue)
            throw new AppBadRequestException("Una devolución no puede incluirse en más de una conciliación.");
        if (saleReturn.StatusId is not ((int)SaleReturnStatusOption.PickedUpAndRefunded or (int)SaleReturnStatusOption.Completed))
            throw new AppBadRequestException("Solo se pueden conciliar devoluciones recogidas por la agencia.");
    }

    private string ResolveUserId() => _currentUserService.UserId ?? "system";

    private static void ValidateDeliveryAmounts(ReconcileSaleDeliveryDTO request)
    {
        if (request.AmountCollectedNio < 0 || request.AmountCollectedUsd < 0 ||
            request.ChangeGivenNio < 0 || request.ShippingPaidToAgency < 0)
        {
            throw new AppBadRequestException("Los montos del envio no pueden ser negativos.");
        }

        if (request.AmountCollectedUsd == 0 && request.CollectionExchangeRate.HasValue)
            throw new AppBadRequestException("La tasa de cambio del cobro solo debe indicarse cuando se recolecten dolares.");
        if (request.AmountCollectedUsd > 0 && (!request.CollectionExchangeRate.HasValue || request.CollectionExchangeRate <= 0))
            throw new AppBadRequestException("Los cobros en dolares deben indicar la tasa de cambio aplicada por la agencia.");
        if (request.ChangeGivenNio > request.AmountCollectedNio + request.AmountCollectedUsd * (request.CollectionExchangeRate ?? 0))
            throw new AppBadRequestException("El vuelto entregado no puede exceder el monto recibido de la clienta.");
    }
}
