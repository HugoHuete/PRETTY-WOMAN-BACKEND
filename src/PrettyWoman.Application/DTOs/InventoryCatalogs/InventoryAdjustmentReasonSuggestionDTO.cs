namespace PrettyWoman.Application.DTOs.InventoryCatalogs;

public class InventoryAdjustmentReasonSuggestionDTO
{
    public int InventoryAdjustmentReasonId { get; set; }
    public required string InventoryAdjustmentReasonName { get; set; }
    public required string Description { get; set; }
    public List<InventoryAdjustmentBucketSuggestionDTO> SuggestedMovements { get; set; } = [];
}
