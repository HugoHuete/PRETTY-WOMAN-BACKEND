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
            await ValidatePaymentDataAsync(paymentMovement.PaymentMethodId, paymentMovement.PaymentTerminalId, paymentMovement.GrossAmount);
            payments.Add(await CreatePaymentMovementAsync(paymentMovement));
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
        await ValidatePaymentDataAsync(paymentMovement.PaymentMethodId, paymentMovement.PaymentTerminalId, paymentMovement.GrossAmount);

        var sale = await GetSaleForPaymentChangesAsync(saleId);
        EnsureSalePaymentMovementsCanBeChanged(sale);

        var payment = await CreatePaymentMovementAsync(paymentMovement);
        var paymentTotal = SalePaymentMovementRules.CalculateTotal(sale.PaymentMovements) + payment.GrossAmount;
        SalePaymentMovementRules.EnsureAllowedTotal(sale.SaleChannelId, sale.Total, paymentTotal);

        sale.PaymentMovements.Add(payment);
        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(sale.Total, paymentTotal);
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, paymentTotal);
        await CreateFinancialMovementsAsync([payment]);
        await SaveWithConcurrencyHandlingAsync();

        return payment.Id;
    }

    public async Task PatchAsync(int saleId, int paymentMovementId, UpdateSalePaymentMovementDTO paymentMovement)
    {
        if (!paymentMovement.HasChanges)
        {
            throw new AppBadRequestException("Debe enviar al menos un campo para actualizar el pago.");
        }

        var sale = await GetSaleForPaymentChangesAsync(saleId);
        EnsureSalePaymentMovementsCanBeChanged(sale);

        var payment = GetPaymentMovement(sale, paymentMovementId);
        EnsurePaymentCanBeUpdated(payment);

        if (paymentMovement.HasMovementDate && !paymentMovement.MovementDate.HasValue)
        {
            throw new AppBadRequestException("La fecha del pago no puede ser nula.");
        }

        if (paymentMovement.HasPaymentMethodId && !paymentMovement.PaymentMethodId.HasValue)
        {
            throw new AppBadRequestException("El metodo de pago no puede ser nulo.");
        }

        if (paymentMovement.HasGrossAmount && !paymentMovement.GrossAmount.HasValue)
        {
            throw new AppBadRequestException("El monto del pago no puede ser nulo.");
        }

        var movementDate = paymentMovement.HasMovementDate ? paymentMovement.MovementDate!.Value : payment.MovementDate;
        var paymentMethodId = paymentMovement.HasPaymentMethodId ? paymentMovement.PaymentMethodId!.Value : payment.PaymentMethodId;
        var paymentTerminalId = paymentMovement.HasPaymentTerminalId ? paymentMovement.PaymentTerminalId : payment.PaymentTerminalId;
        var grossAmount = paymentMovement.HasGrossAmount ? paymentMovement.GrossAmount!.Value : payment.GrossAmount;

        await ValidatePaymentDataAsync(paymentMethodId, paymentTerminalId, grossAmount);

        var currentPaymentTotal = SalePaymentMovementRules.CalculateTotal(sale.PaymentMovements);
        var updatedPaymentTotal = currentPaymentTotal - payment.GrossAmount + grossAmount;
        SalePaymentMovementRules.EnsureAllowedTotal(sale.SaleChannelId, sale.Total, updatedPaymentTotal);

        await ApplyPaymentAmountsAsync(payment, movementDate, paymentMethodId, paymentTerminalId, grossAmount);
        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(sale.Total, updatedPaymentTotal);
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, updatedPaymentTotal);
        await SyncFinancialMovementAsync(payment);
        await SaveWithConcurrencyHandlingAsync();
    }

    public async Task<int> RefundAsync(int saleId, int paymentMovementId, RefundSalePaymentMovementDTO refund)
    {
        var sale = await GetSaleForPaymentChangesAsync(saleId);
        EnsureSalePaymentMovementsCanBeChanged(sale);

        var originalPayment = GetPaymentMovement(sale, paymentMovementId);
        var refundMovement = await CreateRefundPaymentMovementAsync(originalPayment, refund);
        var paymentTotal = SalePaymentMovementRules.CalculateTotal(sale.PaymentMovements) - refundMovement.GrossAmount;
        EnsureRefundDoesNotBreakInStorePaymentRule(sale, paymentTotal);

        RegisterRefund(sale, originalPayment, refundMovement);
        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(sale.Total, paymentTotal);
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, paymentTotal);
        await CreateFinancialMovementsAsync([refundMovement]);
        await SaveWithConcurrencyHandlingAsync();

        return refundMovement.Id;
    }

    public async Task AdjustAsync(int saleId, AdjustSalePaymentMovementsDTO adjustment)
    {
        adjustment.PaymentMovements ??= [];
        adjustment.Refunds ??= [];
        if (adjustment.PaymentMovements.Count == 0 && adjustment.Refunds.Count == 0)
        {
            throw new AppBadRequestException("Debe enviar al menos un pago o un reembolso para ajustar la venta.");
        }

        var sale = await GetSaleForPaymentChangesAsync(saleId);
        EnsureSalePaymentMovementsCanBeChanged(sale);

        var newPayments = await CreateInitialAsync(adjustment.PaymentMovements);
        var refunds = new List<SalePaymentMovement>();
        foreach (var refundRequest in adjustment.Refunds)
        {
            var originalPayment = GetPaymentMovement(sale, refundRequest.PaymentMovementId);
            var refund = await CreateRefundPaymentMovementAsync(originalPayment, refundRequest);
            RegisterRefund(sale, originalPayment, refund);
            refunds.Add(refund);
        }

        sale.PaymentMovements.AddRange(newPayments);
        var paymentTotal = SalePaymentMovementRules.CalculateTotal(sale.PaymentMovements);
        SalePaymentMovementRules.EnsureAllowedTotal(sale.SaleChannelId, sale.Total, paymentTotal);
        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(sale.Total, paymentTotal);
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, paymentTotal);

        await CreateFinancialMovementsAsync(newPayments.Concat(refunds));
        await SaveWithConcurrencyHandlingAsync();
    }

    private async Task<Sale> GetSaleForPaymentChangesAsync(int saleId)
    {
        return await _context.Sales
            .Include(sale => sale.PaymentMovements)
                .ThenInclude(payment => payment.ReversalMovements)
            .AsSplitQuery()
            .FirstOrDefaultAsync(sale => sale.Id == saleId)
            ?? throw new AppNotFoundException($"La venta con id {saleId} no existe.");
    }

    private async Task<SalePaymentMovement> CreatePaymentMovementAsync(CreateSalePaymentMovementDTO paymentRequest)
    {
        var payment = new SalePaymentMovement
        {
            MovementDirectionId = (int)MovementDirectionOptions.In,
            UserId = ResolveUserId()
        };

        await ApplyPaymentAmountsAsync(
            payment,
            paymentRequest.MovementDate ?? DateTime.UtcNow,
            paymentRequest.PaymentMethodId,
            paymentRequest.PaymentTerminalId,
            paymentRequest.GrossAmount);

        return payment;
    }

    private async Task ApplyPaymentAmountsAsync(SalePaymentMovement payment, DateTime movementDate, int paymentMethodId, int? paymentTerminalId, decimal grossAmount)
    {
        var terminal = paymentTerminalId.HasValue
            ? await _context.PaymentTerminals.FirstAsync(item => item.Id == paymentTerminalId.Value && item.Enabled)
            : null;

        var commissionAmount = terminal is null ? 0 : Math.Round(grossAmount * terminal.ComissionPercentage / 100, 2);
        var incomeTaxAmount = terminal is null ? 0 : Math.Round((grossAmount - commissionAmount) * terminal.IncomeTaxPercentage / 100, 2);

        payment.MovementDate = movementDate;
        payment.PaymentMethodId = paymentMethodId;
        payment.PaymentTerminalId = paymentTerminalId;
        payment.GrossAmount = grossAmount;
        payment.CommissionPercentage = terminal?.ComissionPercentage ?? 0;
        payment.CommissionAmount = commissionAmount;
        payment.IncomeTaxPercentage = terminal?.IncomeTaxPercentage ?? 0;
        payment.IncomeTaxAmount = incomeTaxAmount;
        payment.NetReceivedAmount = grossAmount - commissionAmount - incomeTaxAmount;
    }

    private async Task<SalePaymentMovement> CreateRefundPaymentMovementAsync(SalePaymentMovement originalPayment, RefundSalePaymentMovementDTO refundRequest)
    {
        EnsurePaymentCanBeRefunded(originalPayment);

        var refundedAmount = originalPayment.ReversalMovements.Sum(movement => movement.GrossAmount);
        var remainingRefundableAmount = originalPayment.GrossAmount - refundedAmount;
        if (remainingRefundableAmount <= 0)
        {
            throw new AppBadRequestException("El pago ya fue reembolsado completamente.");
        }

        if (originalPayment.PaymentMethodId == (int)PaymentMethodOption.Card)
        {
            return CreateCardRefundPaymentMovement(originalPayment, refundRequest, refundedAmount);
        }

        var paymentMethodId = refundRequest.PaymentMethodId ?? originalPayment.PaymentMethodId;
        if (paymentMethodId == (int)PaymentMethodOption.Card)
        {
            throw new AppBadRequestException("Solo se puede reembolsar por tarjeta cuando el pago original fue por tarjeta.");
        }

        if (refundRequest.PaymentTerminalId.HasValue)
        {
            throw new AppBadRequestException("Los reembolsos en efectivo o transferencia no deben asociarse a una terminal de pago.");
        }

        var grossAmount = refundRequest.GrossAmount ?? remainingRefundableAmount;
        await ValidatePaymentDataAsync(paymentMethodId, null, grossAmount);
        if (grossAmount > remainingRefundableAmount)
        {
            throw new AppBadRequestException("El monto del reembolso no puede exceder el saldo pendiente de reembolsar del pago.");
        }

        return new SalePaymentMovement
        {
            MovementDate = refundRequest.MovementDate ?? DateTime.UtcNow,
            MovementDirectionId = (int)MovementDirectionOptions.Out,
            PaymentMethodId = paymentMethodId,
            ReversedSalePaymentMovementId = originalPayment.Id,
            GrossAmount = grossAmount,
            NetReceivedAmount = grossAmount,
            UserId = ResolveUserId()
        };
    }

    private SalePaymentMovement CreateCardRefundPaymentMovement(SalePaymentMovement originalPayment, RefundSalePaymentMovementDTO refundRequest, decimal refundedAmount)
    {
        if (refundedAmount > 0)
        {
            throw new AppBadRequestException("Los pagos por tarjeta solo se pueden anular una vez y por el monto completo.");
        }

        if (refundRequest.PaymentMethodId.HasValue && refundRequest.PaymentMethodId.Value != originalPayment.PaymentMethodId ||
            refundRequest.PaymentTerminalId.HasValue && refundRequest.PaymentTerminalId.Value != originalPayment.PaymentTerminalId ||
            refundRequest.GrossAmount.HasValue && refundRequest.GrossAmount.Value != originalPayment.GrossAmount)
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
            CommissionPercentage = originalPayment.CommissionPercentage,
            CommissionAmount = originalPayment.CommissionAmount,
            IncomeTaxPercentage = originalPayment.IncomeTaxPercentage,
            IncomeTaxAmount = originalPayment.IncomeTaxAmount,
            NetReceivedAmount = originalPayment.NetReceivedAmount,
            UserId = ResolveUserId()
        };
    }

    private async Task ValidatePaymentDataAsync(int paymentMethodId, int? paymentTerminalId, decimal grossAmount)
    {
        if (grossAmount <= 0)
        {
            throw new AppBadRequestException("El monto de cada pago debe ser mayor que cero.");
        }

        if (!await _context.PaymentMethods.AnyAsync(method => method.Id == paymentMethodId))
        {
            throw new AppNotFoundException($"El metodo de pago con id {paymentMethodId} no existe.");
        }

        var isCard = paymentMethodId == (int)PaymentMethodOption.Card;
        if (isCard && !paymentTerminalId.HasValue)
        {
            throw new AppBadRequestException("Los pagos con tarjeta deben asociarse a una terminal de pago.");
        }

        if (!isCard && paymentTerminalId.HasValue)
        {
            throw new AppBadRequestException("Solo los pagos con tarjeta pueden asociarse a una terminal de pago.");
        }

        if (paymentTerminalId.HasValue && !await _context.PaymentTerminals.AnyAsync(terminal => terminal.Id == paymentTerminalId.Value && terminal.Enabled))
        {
            throw new AppBadRequestException($"La terminal de pago con id {paymentTerminalId.Value} no existe o no esta habilitada.");
        }
    }

    private async Task SyncFinancialMovementAsync(SalePaymentMovement payment)
    {
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

    private static void EnsureSalePaymentMovementsCanBeChanged(Sale sale)
    {
        if (sale.SaleStatusId == (int)SaleStatusOption.Cancelled)
        {
            throw new AppBadRequestException("No se pueden modificar los pagos de una venta cancelada.");
        }
    }

    private static SalePaymentMovement GetPaymentMovement(Sale sale, int paymentMovementId)
    {
        return sale.PaymentMovements.FirstOrDefault(payment => payment.Id == paymentMovementId)
            ?? throw new AppNotFoundException($"El movimiento de pago con id {paymentMovementId} no existe para la venta indicada.");
    }

    private static void EnsurePaymentCanBeUpdated(SalePaymentMovement payment)
    {
        if (payment.MovementDirectionId != (int)MovementDirectionOptions.In || payment.ReversalMovements.Count > 0)
        {
            throw new AppBadRequestException("No se puede actualizar un pago que ya tiene reembolsos o que no es de entrada.");
        }
    }

    private static void EnsurePaymentCanBeRefunded(SalePaymentMovement payment)
    {
        if (payment.MovementDirectionId != (int)MovementDirectionOptions.In)
        {
            throw new AppBadRequestException("Solo se pueden reembolsar pagos de entrada.");
        }
    }

    private static void EnsureRefundDoesNotBreakInStorePaymentRule(Sale sale, decimal paymentTotal)
    {
        if (sale.SaleChannelId == (int)SaleChannelOption.InStoreSale && paymentTotal < sale.Total)
        {
            throw new AppBadRequestException("Las ventas en local no pueden quedar con pago menor al total despues del reembolso.");
        }
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
}
