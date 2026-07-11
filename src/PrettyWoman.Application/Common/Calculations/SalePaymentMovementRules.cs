using PrettyWoman.Application.Exceptions;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Common.Calculations;

public static class SalePaymentMovementRules
{
    public static decimal CalculateProductTotal(IEnumerable<SalePaymentMovement> payments)
        => payments.Sum(payment => SignedAmount(payment, payment.ProductAmount));

    public static decimal CalculateShippingTotal(IEnumerable<SalePaymentMovement> payments, int saleDeliveryId)
        => payments.Where(payment => payment.SaleDeliveryId == saleDeliveryId)
            .Sum(payment => SignedAmount(payment, payment.ShippingAmount));

    public static int ResolveStatus(decimal amountToCharge, decimal productPaymentTotal)
    {
        if (productPaymentTotal == 0) return (int)SalePaymentStatusOption.Unpaid;
        if (productPaymentTotal < amountToCharge) return (int)SalePaymentStatusOption.PartiallyPaid;
        if (productPaymentTotal > amountToCharge) return (int)SalePaymentStatusOption.RefundPending;
        return (int)SalePaymentStatusOption.Paid;
    }

    // AllowOverpayment is used when replacing products after payments were already received.
    public static void EnsureAllowedProductTotal(int saleChannelId, decimal amountToCharge, decimal productPaymentTotal, bool allowOverpayment = false)
    {
        if (!allowOverpayment && productPaymentTotal > amountToCharge)
            throw new AppBadRequestException("La suma de pagos aplicados a productos no puede exceder el total de la venta.");
        if (saleChannelId == (int)SaleChannelOption.InStoreSale && !allowOverpayment && productPaymentTotal != amountToCharge)
            throw new AppBadRequestException("Las ventas en local deben quedar pagadas completamente al momento de registrarse.");
        if (saleChannelId == (int)SaleChannelOption.InStoreSale && allowOverpayment && productPaymentTotal < amountToCharge)
            throw new AppBadRequestException("Las ventas en local deben quedar pagadas completamente al momento de registrarse.");
    }

    private static decimal SignedAmount(SalePaymentMovement payment, decimal amount)
        => payment.MovementDirectionId == (int)MovementDirectionOptions.Out ? -amount : amount;
}