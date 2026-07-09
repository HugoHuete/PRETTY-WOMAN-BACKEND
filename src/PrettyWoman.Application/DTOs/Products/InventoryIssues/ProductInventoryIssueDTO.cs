namespace PrettyWoman.Application.DTOs.Products.InventoryIssues;

public class ProductInventoryIssueDTO
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int ProductDetailId { get; set; }
    public string? ProductName { get; set; }
    public int? ProductCode { get; set; }
    public int SizeId { get; set; }
    public string? SizeName { get; set; }
    public string? Color { get; set; }
    public int ProductInventoryIssueTypeId { get; set; }
    public string? ProductInventoryIssueTypeName { get; set; }
    public int ProductInventoryIssueStatusId { get; set; }
    public string? ProductInventoryIssueStatusName { get; set; }
    public int Quantity { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}