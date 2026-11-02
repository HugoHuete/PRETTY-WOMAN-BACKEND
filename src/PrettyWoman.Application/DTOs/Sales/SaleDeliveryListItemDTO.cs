namespace PrettyWoman.Application.DTOs.Sales;

public class SaleDeliveryListItemDTO
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Code { get; set; } = string.Empty;
    public int DeliveryStatusId { get; set; }
    public string? DeliveryStatusName { get; set; }
    public int DeliveryAgencyId { get; set; }
    public string? DeliveryAgencyName { get; set; }
    public int MunicipalityId { get; set; }
    public string? MunicipalityName { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientPhone { get; set; }
    public string? ClientInstagramUser { get; set; }
    public string? ClientMessengerUser { get; set; }
    public decimal AmountToCollect { get; set; }
    public decimal ShippingChargedToClient { get; set; }
    public decimal ShippingPaidToAgency { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Comments { get; set; }
}
