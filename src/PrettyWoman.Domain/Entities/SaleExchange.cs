using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

/// <summary>
/// A post-sale exchange. The original sale remains immutable and this aggregate
/// records the credit for returned merchandise and all outgoing merchandise.
/// </summary>
public class SaleExchange : IAuditableEntity
{
    public int Id { get; set; }
    public int OriginalSaleId { get; set; }
    public int StatusId { get; set; } = (int)SaleExchangeStatusOption.Requested;
    public decimal RecognizedReturnTotal { get; set; }
    public decimal OutboundItemsTotal { get; set; }
    public decimal BalanceToCollect { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public Sale? OriginalSale { get; set; }
    public List<ExchangeReturnItem> ReturnItems { get; set; } = [];
    public List<ExchangeOutboundItem> OutboundItems { get; set; } = [];
}
