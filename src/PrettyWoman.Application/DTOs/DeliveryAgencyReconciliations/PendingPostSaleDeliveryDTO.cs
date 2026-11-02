namespace PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;

public class PendingPostSaleDeliveryDTO
{
    public int Id { get; set; }
    public int? SaleReturnId { get; set; }
    public int? SaleExchangeId { get; set; }
    public int SaleId { get; set; }
    public int DeliveryAgencyId { get; set; }
    public string? DeliveryAgencyName { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ClientName { get; set; }
    public decimal ShippingChargedToClient { get; set; }
    public decimal ShippingPaidToAgency { get; set; }
    public decimal AmountToCollect { get; set; }
    public DateTime CreatedAt { get; set; }
}
