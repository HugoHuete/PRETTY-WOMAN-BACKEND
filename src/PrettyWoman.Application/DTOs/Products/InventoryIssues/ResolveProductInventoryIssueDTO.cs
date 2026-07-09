namespace PrettyWoman.Application.DTOs.Products.InventoryIssues;

public class ResolveProductInventoryIssueDTO
{
    public int ProductInventoryIssueStatusId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Comments { get; set; }
}