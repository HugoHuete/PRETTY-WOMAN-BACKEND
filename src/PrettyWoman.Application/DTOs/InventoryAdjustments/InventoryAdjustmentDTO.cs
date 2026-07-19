namespace PrettyWoman.Application.DTOs.InventoryAdjustments;

public class InventoryAdjustmentDTO
{
    public int Id { get; set; }
    public int InventoryAdjustmentReasonId { get; set; }
    public string? InventoryAdjustmentReasonName { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<InventoryAdjustmentItemDTO> Items { get; set; } = [];
}
