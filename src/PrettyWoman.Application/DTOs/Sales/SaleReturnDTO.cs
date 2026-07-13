namespace PrettyWoman.Application.DTOs.Sales;

public class SaleReturnDTO
{
    public int Id { get; set; }
    public int OriginalSaleId { get; set; }
    public int ReasonId { get; set; }
    public string? ReasonName { get; set; }
    public int MethodId { get; set; }
    public string? MethodName { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public int? DeliveryAgencyId { get; set; }
    public string? ReturnCode { get; set; }
    public decimal ProductRefundTotal { get; set; }
    public decimal ReturnShippingChargedToClient { get; set; }
    public decimal ReturnShippingPaidToAgency { get; set; }
    public decimal OriginalShippingRefund { get; set; }
    public decimal RefundTotal { get; set; }
    public int? RefundPaymentMethodId { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Comments { get; set; }
    public List<SaleReturnItemDTO> Items { get; set; } = [];
}

public class SaleReturnItemDTO
{
    public int Id { get; set; }
    public int OriginalSaleProductId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal RecognizedUnitAmount { get; set; }
    public decimal OriginalUnitCost { get; set; }
    public int? ProductInventoryIssueId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Comments { get; set; }
}
