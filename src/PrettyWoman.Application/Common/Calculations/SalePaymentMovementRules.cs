using PrettyWoman.Application.Exceptions;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Common.Calculations;

public static class SalePaymentMovementRules
{
    public static decimal CalculateTotal(IEnumerable<SalePaymentMovement> payments) => payments.Sum(payment => payment.MovementDirectionId == (int)MovementDirectionOptions.Out ? -payment.GrossAmount : payment.GrossAmount);

    public static int ResolveStatus(decimal amountToCharge, decimal paymentTotal)
    {
        if (paymentTotal == 0) return (int)SalePaymentStatusOption.Unpaid;
        if (paymentTotal < amountToCharge) return (int)SalePaymentStatusOption.PartiallyPaid;
        if (paymentTotal > amountToCharge) return (int)SalePaymentStatusOption.RefundPending;
        return (int)SalePaymentStatusOption.Paid;
    }

    // AllowOverpayment is used for cases where the sale is being refunded, so the total of payments can exceed the amount to charge.
    public static void EnsureAllowedTotal(int saleChannelId, decimal amountToCharge, decimal paymentTotal, bool allowOverpayment = false)
    {
        if (!allowOverpayment && paymentTotal > amountToCharge)
            throw new AppBadRequestException("La suma de pagos no puede exceder el total de la venta.");
        if (saleChannelId == (int)SaleChannelOption.InStoreSale && !allowOverpayment && paymentTotal != amountToCharge)
            throw new AppBadRequestException("Las ventas en local deben quedar pagadas completamente al momento de registrarse.");
        if (saleChannelId == (int)SaleChannelOption.InStoreSale && allowOverpayment && paymentTotal < amountToCharge)
            throw new AppBadRequestException("Las ventas en local deben quedar pagadas completamente al momento de registrarse.");
    }
}
