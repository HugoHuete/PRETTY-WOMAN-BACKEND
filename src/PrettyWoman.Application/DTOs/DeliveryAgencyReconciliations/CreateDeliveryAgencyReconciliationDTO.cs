namespace PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;

public class CreateDeliveryAgencyReconciliationDTO
{
    public int DeliveryAgencyId { get; set; }
    public DateTime? ReconciliationDate { get; set; }
    public decimal SettlementExchangeRate { get; set; }
    public decimal AmountReceivedNio { get; set; }
    public decimal AmountReceivedUsd { get; set; }
    public decimal AmountPaidToAgencyNio { get; set; }
    public decimal AmountPaidToAgencyUsd { get; set; }
    public string? Comments { get; set; }
    public List<ReconcileSaleDeliveryDTO> Deliveries { get; set; } = [];
}

public class ReconcileSaleDeliveryDTO
{
    public int SaleDeliveryId { get; set; }
    public decimal AmountCollectedNio { get; set; }
    public decimal AmountCollectedUsd { get; set; }
    public decimal ChangeGivenNio { get; set; }
    public decimal? CollectionExchangeRate { get; set; }
    public decimal ShippingPaidToAgency { get; set; }
}