namespace PrettyWoman.Application.DTOs.InventoryAdjustments;

public class InventoryAdjustmentQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? ProductDetailId { get; set; }
    public int? ProductId { get; set; }
    public int? InventoryAdjustmentReasonId { get; set; }
    public int? FromStockBucketId { get; set; }
    public int? ToStockBucketId { get; set; }
    public DateTime? AdjustmentDateFrom { get; set; }
    public DateTime? AdjustmentDateTo { get; set; }
}
