namespace PrettyWoman.Application.DTOs.InventoryAdjustments;

public class CreateInventoryAdjustmentDTO
{
    public int InventoryAdjustmentReasonId { get; set; }
    public DateTime? AdjustmentDate { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }
    public ICollection<CreateInventoryAdjustmentItemDTO> Items { get; set; } = [];
}
