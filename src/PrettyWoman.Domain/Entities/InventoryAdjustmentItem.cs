namespace PrettyWoman.Domain.Entities;

public class InventoryAdjustmentItem
{
    public int Id { get; set; }
    public int InventoryAdjustmentId { get; set; }
    public int ProductId { get; set; }
    public int FromStockBucketId { get; set; }
    public int ToStockBucketId { get; set; }
    public int Quantity { get; set; }
    public string? Comments { get; set; }

    public InventoryAdjustment? InventoryAdjustment { get; set; }
    public Product? Product { get; set; }
    public InventoryStockBucket? FromStockBucket { get; set; }
    public InventoryStockBucket? ToStockBucket { get; set; }
    public InventoryMovement? InventoryMovement { get; set; }
}
