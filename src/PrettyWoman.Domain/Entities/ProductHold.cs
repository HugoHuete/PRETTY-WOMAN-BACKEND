namespace PrettyWoman.Domain.Entities;

using PrettyWoman.Domain.Enums;

public class ProductHold
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? SaleId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public required string HoldReason { get; set; }
    public string? Comments { get; set; }
    public ProductHoldStatus Status { get; set; } = ProductHoldStatus.Active;

    public Product? Product { get; set; }
    public Sale? Sale { get; set; }
}
