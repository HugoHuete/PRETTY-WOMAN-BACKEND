namespace PrettyWoman.Application.DTOs.Sales;

/// <summary>Representa el estado operativo y los importes de un envío de venta.</summary>
public class SaleDeliveryDTO
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Code { get; set; } = string.Empty;
    public int MunicipalityId { get; set; }
    public string? MunicipalityName { get; set; }
    public int DeliveryAgencyId { get; set; }
    public string? DeliveryAgencyName { get; set; }
    public bool DeliveryAgencyCanCollectCashOnDelivery { get; set; }
    public int DeliveryStatusId { get; set; }
    public string? DeliveryStatusName { get; set; }
    public int? ClientId { get; set; }
    public decimal AmountToCollect { get; set; }
    public decimal AmountCollectedNio { get; set; }
    public decimal AmountCollectedUsd { get; set; }
    public decimal ChangeGivenNio { get; set; }
    public decimal? CollectionExchangeRate { get; set; }
    public decimal ShippingChargedToClient { get; set; }
    public decimal ShippingPaidToAgency { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Comments { get; set; }
}
