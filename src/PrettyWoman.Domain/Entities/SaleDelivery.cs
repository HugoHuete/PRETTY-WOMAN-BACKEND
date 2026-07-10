namespace PrettyWoman.Domain.Entities;

public class SaleDelivery
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required string Code { get; set; }
    public int SaleId { get; set; }
    public int MunicipalityId { get; set; }
    public int DeliveryAgencyId { get; set; }
    public int DeliveryStatusId { get; set; }
    public int? ClientId { get; set; }
    public decimal AmountToCollect { get; set; }
    public decimal AmountCollectedNio { get; set; } = 0;
    public decimal AmountCollectedUsd { get; set; } = 0;
    public decimal ChangeAmount { get; set; } = 0; // Vuelto para cuando pague en Usd
    public decimal? ExchangeRate { get; set; }
    public decimal ShippingChargedToClient { get; set; }
    public decimal ShippingPaidToAgency { get; set; }
    public required string UserId { get; set; }
    public string? Comments { get; set; }


    public Sale? Sale { get; set; }
    public Municipality? Municipality { get; set; }
    public DeliveryStatus? DeliveryStatus { get; set; }
    public DeliveryAgency? DeliveryAgency { get; set; }
    public Client? Client { get; set; }

}
