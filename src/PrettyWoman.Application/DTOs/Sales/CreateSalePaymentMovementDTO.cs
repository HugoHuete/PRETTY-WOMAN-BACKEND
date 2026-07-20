namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSalePaymentMovementDTO
{
    public DateTime? MovementDate { get; set; }
    public int PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public decimal ProductAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public int? SaleDeliveryId { get; set; }
    public decimal AmountReceivedNio { get; set; }
    public decimal AmountReceivedUsd { get; set; }
    public decimal ChangeGivenNio { get; set; }
    public decimal? ExchangeRate { get; set; }
}
