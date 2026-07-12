using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

public class ExchangeReturnItem : IAuditableEntity
{
    public int Id { get; set; }
    public int SaleExchangeId { get; set; }
    public int OriginalSaleProductId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal RecognizedUnitAmount { get; set; }
    public decimal OriginalUnitCost { get; set; }
    public int StatusId { get; set; } = (int)ExchangeReturnItemStatusOption.PendingHandover;
    public DateTime? HandedToAgencyAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public SaleExchange? SaleExchange { get; set; }
    public SaleProduct? OriginalSaleProduct { get; set; }
    public Product? Product { get; set; }
}
