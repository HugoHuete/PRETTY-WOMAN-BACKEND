namespace PrettyWoman.Domain.Entities;

public class DeliveryAgencyReconciliation : IAuditableEntity
{
    public int Id { get; set; }
    public int DeliveryAgencyId { get; set; }
    public DateTime ReconciliationDate { get; set; }
    public decimal SettlementExchangeRate { get; set; }
    public decimal AmountReceivedNio { get; set; }
    public decimal AmountReceivedUsd { get; set; }
    public decimal AmountPaidToAgencyNio { get; set; }
    public decimal AmountPaidToAgencyUsd { get; set; }
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public DeliveryAgency? DeliveryAgency { get; set; }
    public List<SaleDelivery> Deliveries { get; set; } = [];
    public List<SalePaymentMovement> PaymentMovements { get; set; } = [];
    public List<FinancialMovement> FinancialMovements { get; set; } = [];
}