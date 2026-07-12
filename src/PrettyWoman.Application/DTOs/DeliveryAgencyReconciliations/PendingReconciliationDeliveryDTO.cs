namespace PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;

public class PendingReconciliationDeliveryDTO
{
    public int SaleDeliveryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int DeliveryStatusId { get; set; }
    public string? DeliveryStatusName { get; set; }
    public int DeliveryAgencyId { get; set; }
    public string? DeliveryAgencyName { get; set; }
    public int SaleId { get; set; }
    public decimal SaleTotal { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? DeliveryAddress { get; set; }
    public decimal AmountToCollect { get; set; }
    public decimal ShippingChargedToClient { get; set; }
}
