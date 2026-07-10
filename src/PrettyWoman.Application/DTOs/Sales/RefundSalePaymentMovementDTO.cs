namespace PrettyWoman.Application.DTOs.Sales;

public class RefundSalePaymentMovementDTO
{
    public DateTime? MovementDate { get; set; }
    public int? PaymentMethodId { get; set; }
    public int? PaymentTerminalId { get; set; }
    public decimal? GrossAmount { get; set; }
}