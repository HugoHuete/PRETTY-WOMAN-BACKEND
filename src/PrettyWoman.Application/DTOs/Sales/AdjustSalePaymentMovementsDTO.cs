namespace PrettyWoman.Application.DTOs.Sales;

public class AdjustSalePaymentMovementsDTO
{
    public List<CreateSalePaymentMovementDTO> PaymentMovements { get; set; } = [];
    public List<CreateSalePaymentRefundDTO> Refunds { get; set; } = [];
}
