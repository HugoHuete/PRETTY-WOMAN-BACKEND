namespace PrettyWoman.Domain.Entities;

using PrettyWoman.Domain.Enums;

public class ProductHold : IAuditableEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? SaleId { get; set; }
    public int Quantity { get; set; }
    public DateTime HoldDate { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public required string HoldReason { get; set; }
    public string? Comments { get; set; }
    public int ProductHoldStatusId { get; set; } = (int)ProductHoldStatusOption.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }
    

    public Product? Product { get; set; }
    public ProductHoldStatus? ProductHoldStatus { get; set; }
    public Sale? Sale { get; set; }
}
