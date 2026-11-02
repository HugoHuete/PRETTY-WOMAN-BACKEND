namespace PrettyWoman.Application.DTOs.Sales;

public class SaleDeliveryQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? DeliveryStatusId { get; set; }
    public int? DeliveryAgencyId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientPhone { get; set; }
    public string? ClientInstagramUser { get; set; }
    public string? ClientMessengerUser { get; set; }
}
