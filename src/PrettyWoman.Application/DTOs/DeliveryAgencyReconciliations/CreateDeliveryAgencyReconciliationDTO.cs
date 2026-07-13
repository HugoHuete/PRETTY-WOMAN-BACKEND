namespace PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;

public class CreateDeliveryAgencyReconciliationDTO
{
    public int DeliveryAgencyId { get; set; }
    public DateTime? ReconciliationDate { get; set; }
    public decimal SettlementExchangeRate { get; set; }
    public string? Comments { get; set; }
    public List<ReconcileSaleDeliveryDTO> Deliveries { get; set; } = [];
    public List<ReconcileSaleReturnDTO> Returns { get; set; } = [];
}

public class ReconcileSaleReturnDTO
{
    public int SaleReturnId { get; set; }
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
