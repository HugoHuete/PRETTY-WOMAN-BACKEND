namespace PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;

public class PendingReconciliationReturnDTO
{
    public int SaleReturnId { get; set; }
    public int OriginalSaleId { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public int DeliveryAgencyId { get; set; }
    public string? DeliveryAgencyName { get; set; }
    public string? ReturnCode { get; set; }
    public string? ClientName { get; set; }
    public decimal ProductRefundTotal { get; set; }
    public decimal ReturnShippingChargedToClient { get; set; }
    public decimal ReturnShippingPaidToAgency { get; set; }
    public decimal OriginalShippingRefund { get; set; }
    public decimal RefundTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Comments { get; set; }
}
