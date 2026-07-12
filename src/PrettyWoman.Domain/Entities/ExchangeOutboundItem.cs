namespace PrettyWoman.Domain.Entities;

public class ExchangeOutboundItem : IAuditableEntity
{
    public int Id { get; set; }
    public int SaleExchangeId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int ItemTypeId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TotalCost { get; set; }
    public bool Delivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public SaleExchange? SaleExchange { get; set; }
    public Product? Product { get; set; }
}
