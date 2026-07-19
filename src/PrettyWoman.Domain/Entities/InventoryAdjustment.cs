namespace PrettyWoman.Domain.Entities;

public class InventoryAdjustment : IAuditableEntity
{
    public int Id { get; set; }
    public int InventoryAdjustmentReasonId { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public InventoryAdjustmentReason? InventoryAdjustmentReason { get; set; }
    public ICollection<InventoryAdjustmentItem> Items { get; set; } = [];
}
