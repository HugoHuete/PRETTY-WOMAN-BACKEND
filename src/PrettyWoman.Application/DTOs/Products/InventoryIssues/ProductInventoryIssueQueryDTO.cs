namespace PrettyWoman.Application.DTOs.Products.InventoryIssues;

public class ProductInventoryIssueQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? ProductDetailId { get; set; }
    public int? ProductId { get; set; }
    public int? ProductInventoryIssueTypeId { get; set; }
    public int? ProductInventoryIssueStatusId { get; set; }
}