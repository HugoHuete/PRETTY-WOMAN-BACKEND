namespace PrettyWoman.Application.DTOs.Products.InventoryIssues;

public class CreateProductInventoryIssueDTO
{
    public int ProductId { get; set; }
    public int ProductInventoryIssueTypeId { get; set; }
    public int Quantity { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? Comments { get; set; }
}