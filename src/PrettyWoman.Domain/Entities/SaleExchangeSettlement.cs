namespace PrettyWoman.Domain.Entities;

/// <summary>
/// Records the settlement of the balance resulting from an in-store exchange.
/// The direction indicates whether the customer pays the store or the store refunds the customer.
/// </summary>
public class SaleExchangeSettlement : IAuditableEntity
{
    public int Id { get; set; }
    public int SaleExchangeId { get; set; }
    public int MovementDirectionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime MovementDate { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public SaleExchange? SaleExchange { get; set; }

    /// <summary>
    /// Payment method breakdown for this settlement; multiple rows support split payments.
    /// </summary>
    public List<SaleExchangeSettlementTender> Tenders { get; set; } = [];
}
