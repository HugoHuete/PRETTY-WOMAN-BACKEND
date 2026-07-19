namespace PrettyWoman.Application.DTOs.InventoryCatalogs;

public class InventoryAdjustmentBucketSuggestionDTO
{
    public int FromStockBucketId { get; set; }
    public required string FromStockBucketName { get; set; }
    public int ToStockBucketId { get; set; }
    public required string ToStockBucketName { get; set; }
    public required string Description { get; set; }
}
