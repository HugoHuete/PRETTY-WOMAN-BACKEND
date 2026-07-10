namespace PrettyWoman.Application.DTOs.Sales;

public class CreateSalePaymentRefundDTO : RefundSalePaymentMovementDTO
{
    public int PaymentMovementId { get; set; }
}
