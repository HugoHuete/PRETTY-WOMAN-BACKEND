namespace PrettyWoman.Application.DTOs.Sales;

public class RefundSalePaymentMovementDTO
{
    public DateTime? MovementDate { get; set; }
    public int? PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public decimal? ProductAmount { get; set; }
    public decimal? ShippingAmount { get; set; }
}