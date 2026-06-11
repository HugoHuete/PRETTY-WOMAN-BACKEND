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
    public int ProductHoldStatusId { get; set; } = (int) ProductHoldStatusOption.Active;

    public Product? Product { get; set; }
    public ProductHoldStatus? ProductHoldStatus { get; set; }
    public Sale? Sale { get; set; }
}
