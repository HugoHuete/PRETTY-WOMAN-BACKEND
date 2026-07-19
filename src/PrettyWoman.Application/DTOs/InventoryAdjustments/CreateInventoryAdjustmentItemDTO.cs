namespace PrettyWoman.Application.DTOs.InventoryAdjustments;

public class CreateInventoryAdjustmentItemDTO
{
    public int ProductId { get; set; }
    public int FromStockBucketId { get; set; }
    public int ToStockBucketId { get; set; }
    public int Quantity { get; set; }
    public string? Comments { get; set; }
}
