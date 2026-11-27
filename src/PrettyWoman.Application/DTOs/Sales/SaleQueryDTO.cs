namespace PrettyWoman.Application.DTOs.Sales;

public class SaleQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? SaleStatusId { get; set; }
    public int? SalePaymentStatusId { get; set; }
    public int? SaleChannelId { get; set; }
    public int? DeliveryStatusId { get; set; }
    public int? ClientId { get; set; }
    public int? ProductCode { get; set; }
    public string? ClientName { get; set; }
    public string? ClientPhone { get; set; }
    public string? ClientInstagramUser { get; set; }
    public string? ClientMessengerUser { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
