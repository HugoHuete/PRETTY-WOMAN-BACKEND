namespace PrettyWoman.Application.DTOs.Sales;

public class PostSaleDeliveryDTO
{
    public int Id { get; set; }
    public int? SaleReturnId { get; set; }
    public int? SaleExchangeId { get; set; }
    public int DeliveryAgencyId { get; set; }
    public string? DeliveryAgencyName { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public decimal ShippingChargedToClient { get; set; }
    public decimal ShippingPaidToAgency { get; set; }
    public decimal AmountToCollect { get; set; }
    public decimal AmountCollectedNio { get; set; }
    public decimal AmountCollectedUsd { get; set; }
    public decimal ChangeGivenNio { get; set; }
    public decimal? CollectionExchangeRate { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
}
