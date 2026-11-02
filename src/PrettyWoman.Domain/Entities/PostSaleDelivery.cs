namespace PrettyWoman.Domain.Entities;

public class PostSaleDelivery : IAuditableEntity
{
    public int Id { get; set; }
    public int? SaleReturnId { get; set; }
    public int? SaleExchangeId { get; set; }
    public int DeliveryAgencyId { get; set; }
    public int StatusId { get; set; }
    public string? Code { get; set; }
    public string? Address { get; set; }
    public decimal ShippingChargedToClient { get; set; }
    public decimal ShippingPaidToAgency { get; set; }
    public decimal AmountToCollect { get; set; }
    public decimal AmountCollectedNio { get; set; }
    public decimal AmountCollectedUsd { get; set; }
    public decimal ChangeGivenNio { get; set; }
    public decimal? CollectionExchangeRate { get; set; }
    public int? DeliveryAgencyReconciliationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Comments { get; set; }

    public SaleReturn? SaleReturn { get; set; }
    public SaleExchange? SaleExchange { get; set; }
    public DeliveryAgency? DeliveryAgency { get; set; }
    public DeliveryAgencyReconciliation? DeliveryAgencyReconciliation { get; set; }
}
