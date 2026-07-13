using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Calculations;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class SalePaymentMovementService(IApplicationDbContext context, ICurrentUserService currentUserService, ISaleDeliveryService deliveryService) : ISalePaymentMovementService
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ISaleDeliveryService _deliveryService = deliveryService;

    public async Task<List<SalePaymentMovement>> CreateInitialAsync(List<CreateSalePaymentMovementDTO> paymentMovements)
    {
        var payments = new List<SalePaymentMovement>();
        foreach (var paymentMovement in paymentMovements)
        {
            var allocation = ResolveCreateAllocation(paymentMovement);
            if (allocation.ShippingAmount > 0)
                throw new AppBadRequestException("Los pagos iniciales no pueden aplicarse a envio. Cree primero el envio.");

            await ValidatePaymentDataAsync(paymentMovement.PaymentMethodId, paymentMovement.PaymentTerminalId, allocation.GrossAmount);
            payments.Add(await CreatePaymentMovementAsync(paymentMovement, allocation));
        }

        return payments;
    }

    public async Task CreateFinancialMovementsAsync(IEnumerable<SalePaymentMovement> paymentMovements)
    {
        foreach (var paymentMovement in paymentMovements)
        {
            var exchangeRate = await GetExchangeRateAsync(paymentMovement.MovementDate);
            await _context.FinancialMovements.AddAsync(CreateFinancialMovement(paymentMovement, exchangeRate));
        }
    }

    public async Task<int> AddAsync(int saleId, CreateSalePaymentMovementDTO paymentMovement)
    {
        var allocation = ResolveCreateAllocation(paymentMovement);
        await ValidatePaymentDataAsync(paymentMovement.PaymentMethodId, paymentMovement.PaymentTerminalId, allocation.GrossAmount);

        var sale = await GetSaleForPaymentChangesAsync(saleId);
        EnsureSaleIsNotCancelled(sale);
        await ValidateShippingAllocationAsync(sale, allocation);

        // Un pago conserva sus dos destinos aunque genere un solo movimiento financiero.
        var payment = await CreatePaymentMovementAsync(paymentMovement, allocation);
        sale.PaymentMovements.Add(payment);
        // Cada cambio se valida contra los limites de productos y de cada envio antes de guardar.
        await ValidatePaymentTotalsAsync(sale);

        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(sale.Total, SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements));
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, sale.PaymentMovements);
        await CreateFinancialMovementsAsync([payment]);
        await SaveWithConcurrencyHandlingAsync();

        return payment.Id;
    }

    public async Task PatchAsync(int saleId, int paymentMovementId, UpdateSalePaymentMovementDTO paymentMovement)
    {
        if (!paymentMovement.HasChanges)
            throw new AppBadRequestException("Debe enviar al menos un campo para actualizar el pago.");

        var sale = await GetSaleForPaymentChangesAsync(saleId);
        EnsureSaleIsNotCancelled(sale);

        var payment = GetPaymentMovement(sale, paymentMovementId);
        EnsurePaymentCanBeUpdated(payment);

        if (paymentMovement.HasMovementDate && !paymentMovement.MovementDate.HasValue)
            throw new AppBadRequestException("La fecha del pago no puede ser nula.");
        if (paymentMovement.HasPaymentMethodId && !paymentMovement.PaymentMethodId.HasValue)
            throw new AppBadRequestException("El metodo de pago no puede ser nulo.");

        var movementDate = paymentMovement.HasMovementDate ? paymentMovement.MovementDate!.Value : payment.MovementDate;
        var paymentMethodId = paymentMovement.HasPaymentMethodId ? paymentMovement.PaymentMethodId!.Value : payment.PaymentMethodId;
        var paymentTerminalId = paymentMovement.HasPaymentTerminalId ? paymentMovement.PaymentTerminalId : payment.PaymentTerminalId;
        var allocation = ResolveUpdatedAllocation(payment, paymentMovement);

        await ValidatePaymentDataAsync(paymentMethodId, paymentTerminalId, allocation.GrossAmount);
        await ValidateShippingAllocationAsync(sale, allocation);

        await ApplyPaymentAmountsAsync(payment, movementDate, paymentMethodId, paymentTerminalId, allocation);
        // Cada cambio se valida contra los limites de productos y de cada envio antes de guardar.
        await ValidatePaymentTotalsAsync(sale);

        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(sale.Total, SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements));
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, sale.PaymentMovements);
        await SyncFinancialMovementAsync(payment);
        await SaveWithConcurrencyHandlingAsync();
    }

    public async Task<int> RefundAsync(int saleId, int paymentMovementId, RefundSalePaymentMovementDTO refund)
    {
        var sale = await GetSaleForPaymentChangesAsync(saleId);
        EnsureSaleIsNotCancelled(sale);

        var originalPayment = GetPaymentMovement(sale, paymentMovementId);
        // El reembolso es un movimiento de salida enlazado al pago original; nunca se elimina el pago.
        var refundMovement = await CreateRefundPaymentMovementAsync(originalPayment, refund);
        RegisterRefund(sale, originalPayment, refundMovement);
        // Cada cambio se valida contra los limites de productos y de cada envio antes de guardar.
        await ValidatePaymentTotalsAsync(sale);

        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(sale.Total, SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements));
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, sale.PaymentMovements);
        await CreateFinancialMovementsAsync([refundMovement]);
        await SaveWithConcurrencyHandlingAsync();

        return refundMovement.Id;
    }
    private async Task<Sale> GetSaleForPaymentChangesAsync(int saleId)
    {
        return await _context.Sales
            .Include(sale => sale.PaymentMovements)
                .ThenInclude(payment => payment.ReversalMovements)
            .Include(sale => sale.Deliveries)
            .AsSplitQuery()
            .FirstOrDefaultAsync(sale => sale.Id == saleId)
            ?? throw new AppNotFoundException($"La venta con id {saleId} no existe.");
    }

    private async Task<SalePaymentMovement> CreatePaymentMovementAsync(CreateSalePaymentMovementDTO paymentRequest, PaymentAllocation allocation)
    {
        var payment = new SalePaymentMovement
        {
            MovementDirectionId = (int)MovementDirectionOptions.In,
            UserId = ResolveUserId()
        };

        await ApplyPaymentAmountsAsync(payment, paymentRequest.MovementDate ?? DateTime.UtcNow, paymentRequest.PaymentMethodId, paymentRequest.PaymentTerminalId, allocation);
        return payment;
    }

    private async Task ApplyPaymentAmountsAsync(SalePaymentMovement payment, DateTime movementDate, int paymentMethodId, int? paymentTerminalId, PaymentAllocation allocation)
    {
        var terminal = paymentTerminalId.HasValue
            ? await _context.PaymentTerminals.FirstAsync(item => item.Id == paymentTerminalId.Value && item.Enabled)
            : null;

        var commissionAmount = terminal is null ? 0 : Math.Round(allocation.GrossAmount * terminal.ComissionPercentage / 100, 2);
        var incomeTaxAmount = terminal is null ? 0 : Math.Round((allocation.GrossAmount - commissionAmount) * terminal.IncomeTaxPercentage / 100, 2);

        payment.MovementDate = movementDate;
        payment.PaymentMethodId = paymentMethodId;
        payment.PaymentTerminalId = paymentTerminalId;
        payment.GrossAmount = allocation.GrossAmount;
        payment.ProductAmount = allocation.ProductAmount;
        payment.ShippingAmount = allocation.ShippingAmount;
        payment.SaleDeliveryId = allocation.SaleDeliveryId;
        payment.CommissionPercentage = terminal?.ComissionPercentage ?? 0;
        payment.CommissionAmount = commissionAmount;
        payment.IncomeTaxPercentage = terminal?.IncomeTaxPercentage ?? 0;
        payment.IncomeTaxAmount = incomeTaxAmount;
        payment.NetReceivedAmount = allocation.GrossAmount - commissionAmount - incomeTaxAmount;
    }

    private async Task<SalePaymentMovement> CreateRefundPaymentMovementAsync(SalePaymentMovement originalPayment, RefundSalePaymentMovementDTO refundRequest)
    {
        EnsurePaymentCanBeRefunded(originalPayment); // Only payments of type "In" can be refunded.

        // Como un pago puede tener varios reembolsos, se calcula el monto restante de cada componente del pago original.
        // Cada componente tiene su propio saldo reembolsable; no se compensa envio con productos.
        var refundedProductAmount = originalPayment.ReversalMovements.Sum(movement => movement.ProductAmount);
        var refundedShippingAmount = originalPayment.ReversalMovements.Sum(movement => movement.ShippingAmount);
        var remainingProductAmount = originalPayment.ProductAmount - refundedProductAmount;
        var remainingShippingAmount = originalPayment.ShippingAmount - refundedShippingAmount;

        if (remainingProductAmount + remainingShippingAmount <= 0)
            throw new AppBadRequestException("El pago ya fue reembolsado completamente.");

        if (originalPayment.PaymentMethodId == (int)PaymentMethodOption.Card)
            return CreateCardRefundPaymentMovement(originalPayment, refundRequest, refundedProductAmount, refundedShippingAmount);

        var paymentMethodId = refundRequest.PaymentMethodId ?? originalPayment.PaymentMethodId;
        if (paymentMethodId == (int)PaymentMethodOption.Card)
            throw new AppBadRequestException("Solo se puede reembolsar por tarjeta cuando el pago original fue por tarjeta.");
        if (refundRequest.PaymentTerminalId.HasValue)
            throw new AppBadRequestException("Los reembolsos en efectivo o transferencia no deben asociarse a una terminal de pago.");

        // Se calcula el monto a reembolsar de cada componente del pago original, validando que no exceda el saldo pendiente.
        var allocation = ResolveRefundAllocation(refundRequest, remainingProductAmount, remainingShippingAmount, originalPayment.SaleDeliveryId);
        await ValidatePaymentDataAsync(paymentMethodId, null, allocation.GrossAmount);

        return new SalePaymentMovement
        {
            MovementDate = refundRequest.MovementDate ?? DateTime.UtcNow,
            MovementDirectionId = (int)MovementDirectionOptions.Out,
            PaymentMethodId = paymentMethodId,
            ReversedSalePaymentMovementId = originalPayment.Id,
            GrossAmount = allocation.GrossAmount,
            ProductAmount = allocation.ProductAmount,
            ShippingAmount = allocation.ShippingAmount,
            SaleDeliveryId = allocation.SaleDeliveryId,
            NetReceivedAmount = allocation.GrossAmount,
            UserId = ResolveUserId()
        };
    }

    private SalePaymentMovement CreateCardRefundPaymentMovement(SalePaymentMovement originalPayment, RefundSalePaymentMovementDTO refundRequest, decimal refundedProductAmount, decimal refundedShippingAmount)
    {
        if (refundedProductAmount > 0 || refundedShippingAmount > 0)
            throw new AppBadRequestException("Los pagos por tarjeta solo se pueden anular una vez y por el monto completo.");

        if (refundRequest.PaymentMethodId.HasValue && refundRequest.PaymentMethodId.Value != originalPayment.PaymentMethodId ||
            refundRequest.PaymentTerminalId.HasValue && refundRequest.PaymentTerminalId.Value != originalPayment.PaymentTerminalId ||
            refundRequest.ProductAmount.HasValue && refundRequest.ProductAmount.Value != originalPayment.ProductAmount ||
            refundRequest.ShippingAmount.HasValue && refundRequest.ShippingAmount.Value != originalPayment.ShippingAmount)
        {
            throw new AppBadRequestException("Los pagos por tarjeta solo se pueden anular por el monto completo usando el mismo metodo y terminal.");
        }

        return new SalePaymentMovement
        {
            MovementDate = refundRequest.MovementDate ?? DateTime.UtcNow,
            MovementDirectionId = (int)MovementDirectionOptions.Out,
            PaymentMethodId = originalPayment.PaymentMethodId,
            PaymentTerminalId = originalPayment.PaymentTerminalId,
            ReversedSalePaymentMovementId = originalPayment.Id,
            GrossAmount = originalPayment.GrossAmount,
            ProductAmount = originalPayment.ProductAmount,
            ShippingAmount = originalPayment.ShippingAmount,
            SaleDeliveryId = originalPayment.SaleDeliveryId,
            CommissionPercentage = originalPayment.CommissionPercentage,
            CommissionAmount = originalPayment.CommissionAmount,
            IncomeTaxPercentage = originalPayment.IncomeTaxPercentage,
            IncomeTaxAmount = originalPayment.IncomeTaxAmount,
            NetReceivedAmount = originalPayment.NetReceivedAmount,
            UserId = ResolveUserId()
        };
    }

    private async Task ValidatePaymentTotalsAsync(Sale sale)
    {
        // El estado de pago de la venta depende solo de productos; envio se valida por cada entrega.
        var productTotal = SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements);
        SalePaymentMovementRules.EnsureAllowedProductTotal(sale.SaleChannelId, sale.Total, productTotal);

        if (sale.SaleChannelId == (int)SaleChannelOption.InStoreSale && sale.PaymentMovements.Any(payment => payment.ShippingAmount > 0))
            throw new AppBadRequestException("Las ventas en local no pueden tener pagos aplicados a envio.");

        foreach (var deliveryId in sale.PaymentMovements.Where(payment => payment.SaleDeliveryId.HasValue).Select(payment => payment.SaleDeliveryId!.Value).Distinct())
        {
            var delivery = sale.Deliveries.FirstOrDefault(item => item.Id == deliveryId)
                ?? throw new AppBadRequestException("El pago debe aplicarse a un envio de la misma venta.");
            var shippingTotal = SalePaymentMovementRules.CalculateShippingTotal(sale.PaymentMovements, deliveryId);
            if (shippingTotal > delivery.ShippingChargedToClient)
                throw new AppBadRequestException("La suma de pagos aplicados al envio no puede exceder el monto cobrado a la clienta.");
        }
    }

    private Task ValidateShippingAllocationAsync(Sale sale, PaymentAllocation allocation)
    {
        if (allocation.ShippingAmount == 0 && allocation.SaleDeliveryId.HasValue)
            throw new AppBadRequestException("Solo los pagos aplicados a envio pueden asociarse a un envio.");
        if (allocation.ShippingAmount > 0 && !allocation.SaleDeliveryId.HasValue)
            throw new AppBadRequestException("Los pagos aplicados a envio deben indicar el envio correspondiente.");
        if (allocation.SaleDeliveryId.HasValue && sale.Deliveries.All(delivery => delivery.Id != allocation.SaleDeliveryId.Value))
            throw new AppBadRequestException("El envio indicado no pertenece a la venta.");

        return Task.CompletedTask;
    }

    private static PaymentAllocation ResolveCreateAllocation(CreateSalePaymentMovementDTO request)
    {
        if (request.ProductAmount < 0 || request.ShippingAmount < 0)
            throw new AppBadRequestException("Los montos aplicados a productos y envio no pueden ser negativos.");
        if (request.ProductAmount + request.ShippingAmount <= 0)
            throw new AppBadRequestException("El pago debe aplicar un monto mayor que cero a productos o envio.");

        return new PaymentAllocation(request.ProductAmount, request.ShippingAmount, request.SaleDeliveryId);
    }

    private static PaymentAllocation ResolveUpdatedAllocation(SalePaymentMovement payment, UpdateSalePaymentMovementDTO request)
    {
        if (request.HasProductAmount && (!request.ProductAmount.HasValue || request.ProductAmount.Value < 0) ||
            request.HasShippingAmount && (!request.ShippingAmount.HasValue || request.ShippingAmount.Value < 0))
        {
            throw new AppBadRequestException("Los montos aplicados a productos y envio no pueden ser nulos ni negativos.");
        }

        var productAmount = request.HasProductAmount ? request.ProductAmount!.Value : payment.ProductAmount;
        var shippingAmount = request.HasShippingAmount ? request.ShippingAmount!.Value : payment.ShippingAmount;
        var saleDeliveryId = request.HasSaleDeliveryId ? request.SaleDeliveryId : payment.SaleDeliveryId;
        if (productAmount + shippingAmount <= 0)
            throw new AppBadRequestException("El pago debe aplicar un monto mayor que cero a productos o envio.");

        return new PaymentAllocation(productAmount, shippingAmount, saleDeliveryId);
    }

    private static PaymentAllocation ResolveRefundAllocation(RefundSalePaymentMovementDTO request, decimal remainingProductAmount, decimal remainingShippingAmount, int? saleDeliveryId)
    {
        if (!request.ProductAmount.HasValue && !request.ShippingAmount.HasValue)
            return new PaymentAllocation(remainingProductAmount, remainingShippingAmount, saleDeliveryId);

        var productAmount = request.ProductAmount ?? 0;
        var shippingAmount = request.ShippingAmount ?? 0;
        if (productAmount < 0 || shippingAmount < 0 || productAmount + shippingAmount <= 0)
            throw new AppBadRequestException("El monto del reembolso debe ser mayor que cero.");
        if (productAmount > remainingProductAmount || shippingAmount > remainingShippingAmount)
            throw new AppBadRequestException("El reembolso no puede exceder el saldo pendiente de cada componente del pago.");

        return new PaymentAllocation(productAmount, shippingAmount, shippingAmount > 0 ? saleDeliveryId : null);
    }

    private async Task ValidatePaymentDataAsync(int paymentMethodId, int? paymentTerminalId, decimal grossAmount)
    {
        if (grossAmount <= 0)
            throw new AppBadRequestException("El monto de cada pago debe ser mayor que cero.");
        if (!await _context.PaymentMethods.AnyAsync(method => method.Id == paymentMethodId))
            throw new AppNotFoundException($"El metodo de pago con id {paymentMethodId} no existe.");

        var isCard = paymentMethodId == (int)PaymentMethodOption.Card;
        if (isCard && !paymentTerminalId.HasValue)
            throw new AppBadRequestException("Los pagos con tarjeta deben asociarse a una terminal de pago.");
        if (!isCard && paymentTerminalId.HasValue)
            throw new AppBadRequestException("Solo los pagos con tarjeta pueden asociarse a una terminal de pago.");
        if (paymentTerminalId.HasValue && !await _context.PaymentTerminals.AnyAsync(terminal => terminal.Id == paymentTerminalId.Value && terminal.Enabled))
            throw new AppBadRequestException($"La terminal de pago con id {paymentTerminalId.Value} no existe o no esta habilitada.");
    }

    private async Task SyncFinancialMovementAsync(SalePaymentMovement payment)
    {
        // Mantiene el movimiento financiero directo alineado cuando cambian monto o fecha del pago.
        var movement = await _context.FinancialMovements.SingleOrDefaultAsync(item => item.SalePaymentMovementId == payment.Id);
        if (movement is null)
        {
            await CreateFinancialMovementsAsync([payment]);
            return;
        }

        movement.MovementDate = payment.MovementDate;
        movement.Amount = payment.NetReceivedAmount;
        movement.ExchangeRate = await GetExchangeRateAsync(payment.MovementDate);
    }

    private async Task<decimal> GetExchangeRateAsync(DateTime movementDate)
    {
        var exchangeRate = await _context.DollarExchangeRates
            .Where(rate => rate.Enabled && rate.StartDate <= movementDate)
            .OrderByDescending(rate => rate.StartDate)
            .Select(rate => (decimal?)rate.BankRate)
            .FirstOrDefaultAsync();
        return exchangeRate ?? throw new AppBadRequestException("No existe una tasa de cambio bancaria habilitada para la fecha del movimiento financiero.");
    }

    private static FinancialMovement CreateFinancialMovement(SalePaymentMovement payment, decimal exchangeRate)
    {
        var isIncome = payment.MovementDirectionId == (int)MovementDirectionOptions.In;
        return new FinancialMovement
        {
            Description = isIncome ? "Pago de venta." : "Reembolso de venta.",
            MovementDate = payment.MovementDate,
            MovementDirectionId = payment.MovementDirectionId,
            FinancialMovementTypeId = isIncome ? (int)FinancialMovementTypeOption.SalePayment : (int)FinancialMovementTypeOption.CustomerRefund,
            SalePaymentMovement = payment,
            Amount = payment.NetReceivedAmount,
            ExchangeRate = exchangeRate
        };
    }

    private static void EnsureSaleIsNotCancelled(Sale sale)
    {
        if (sale.SaleStatusId == (int)SaleStatusOption.Cancelled)
            throw new AppBadRequestException("No se pueden modificar los pagos de una venta cancelada.");
    }

    private static SalePaymentMovement GetPaymentMovement(Sale sale, int paymentMovementId)
        => sale.PaymentMovements.FirstOrDefault(payment => payment.Id == paymentMovementId)
            ?? throw new AppNotFoundException($"El movimiento de pago con id {paymentMovementId} no existe para la venta indicada.");

    private static void EnsurePaymentCanBeUpdated(SalePaymentMovement payment)
    {
        if (payment.MovementDirectionId != (int)MovementDirectionOptions.In || payment.ReversalMovements.Count > 0)
            throw new AppBadRequestException("No se puede actualizar un pago que ya tiene reembolsos o que no es de entrada.");
    }

    private static void EnsurePaymentCanBeRefunded(SalePaymentMovement payment)
    {
        if (payment.MovementDirectionId != (int)MovementDirectionOptions.In)
            throw new AppBadRequestException("Solo se pueden reembolsar pagos de entrada.");
    }

    private static void RegisterRefund(Sale sale, SalePaymentMovement originalPayment, SalePaymentMovement refund)
    {
        originalPayment.ReversalMovements.Add(refund);
        sale.PaymentMovements.Add(refund);
        originalPayment.UpdatedAt = DateTime.UtcNow;
    }

    private async Task SaveWithConcurrencyHandlingAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppBadRequestException("El pago fue modificado por otra operacion. Actualice la venta e intente nuevamente.");
        }
    }

    private string ResolveUserId() => _currentUserService.UserId ?? "system";

    private sealed record PaymentAllocation(decimal ProductAmount, decimal ShippingAmount, int? SaleDeliveryId)
    {
        public decimal GrossAmount => ProductAmount + ShippingAmount;
    }
}