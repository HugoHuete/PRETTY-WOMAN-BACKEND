namespace PrettyWoman.Application.DTOs.Sales;

public class SalePaymentMovementDTO
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }
    public int MovementDirectionId { get; set; }
    public int PaymentMethodId { get; set; }
    public string? PaymentMethodName { get; set; }
    public int? PaymentTerminalId { get; set; }
    public string? PaymentTerminalName { get; set; }
    public int? ReversedSalePaymentMovementId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal ProductAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public int? SaleDeliveryId { get; set;}
    public int? DeliveryAgencyReconciliationId { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal IncomeTaxPercentage { get; set; }
    public decimal IncomeTaxAmount { get; set; }
    public decimal NetReceivedAmount { get; set; }
    public decimal AmountReceivedNio { get; set; }
    public decimal AmountReceivedUsd { get; set; }
    public decimal ChangeGivenNio { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal ExchangeDifferenceNio { get; set; }
}
