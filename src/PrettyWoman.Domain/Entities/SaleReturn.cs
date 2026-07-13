using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

public class SaleReturn : IAuditableEntity
{
    public int Id { get; set; }
    public int OriginalSaleId { get; set; }
    public int ReasonId { get; set; }
    public int MethodId { get; set; }
    public int StatusId { get; set; } = (int)SaleReturnStatusOption.Requested;
    public int? DeliveryAgencyId { get; set; }
    public string? ReturnCode { get; set; }
    public decimal ProductRefundTotal { get; set; }
    public decimal ReturnShippingChargedToClient { get; set; }
    public decimal ReturnShippingPaidToAgency { get; set; }
    public decimal OriginalShippingRefund { get; set; }
    public decimal RefundTotal { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public Sale? OriginalSale { get; set; }
    public DeliveryAgency? DeliveryAgency { get; set; }
    public List<SaleReturnItem> Items { get; set; } = [];
}
