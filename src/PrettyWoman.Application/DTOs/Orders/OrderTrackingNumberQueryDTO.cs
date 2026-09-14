namespace PrettyWoman.Application.DTOs.Orders;

public class OrderTrackingNumberQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? IsReceived { get; set; }
    public string? TrackingNumber { get; set; }
    public int? ShippingCompanyId { get; set; }
    public int? OrderStatusId { get; set; }
    public DateTime? PurchaseDateFrom { get; set; }
    public DateTime? PurchaseDateTo { get; set; }
}
