namespace PrettyWoman.Domain.Entities;

public class InventoryMovement : IAuditableEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int InventoryMovementTypeId { get; set; }
    public int FromStockBucketId { get; set; }
    public int ToStockBucketId { get; set; }
    public int Quantity { get; set; }
    public int? OrderId { get; set; }
    public int? SaleProductId { get; set; }
    public int? ProductHoldId { get; set; }
    public int? ProductInventoryIssueId { get; set; }
    public string? Comments { get; set; }

    public DateTime MovementDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }




    public InventoryMovementType? InventoryMovementType { get; set; }
    public InventoryStockBucket? FromStockBucket { get; set; }
    public InventoryStockBucket? ToStockBucket { get; set; }
    public Product? Product { get; set; }
    public Order? Order { get; set; }
    public SaleProduct? SaleProduct { get; set; }
    public ProductHold? ProductHold { get; set; }
    public ProductInventoryIssue? ProductInventoryIssue { get; set; }


}
