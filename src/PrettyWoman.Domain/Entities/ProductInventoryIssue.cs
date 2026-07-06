namespace PrettyWoman.Domain.Entities;

using PrettyWoman.Domain.Enums;

public class ProductInventoryIssue : IAuditableEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int ProductInventoryIssueTypeId { get; set; }
    public int ProductInventoryIssueStatusId { get; set; } = (int)ProductInventoryIssueStatusOption.Open;
    public int Quantity { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public Product? Product { get; set; }
    public ProductInventoryIssueType? ProductInventoryIssueType { get; set; }
    public ProductInventoryIssueStatus? ProductInventoryIssueStatus { get; set; }
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
}
