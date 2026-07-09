namespace PrettyWoman.Application.DTOs.Products;

public class ProductInventoryMovementDTO
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int ProductDetailId { get; set; }
    public string? ProductName { get; set; }
    public int? ProductCode { get; set; }
    public int SizeId { get; set; }
    public string? SizeName { get; set; }
    public string? Color { get; set; }
    public DateTime MovementDate { get; set; }
    public int InventoryMovementTypeId { get; set; }
    public string? InventoryMovementTypeName { get; set; }
    public int FromStockBucketId { get; set; }
    public string? FromStockBucketName { get; set; }
    public int ToStockBucketId { get; set; }
    public string? ToStockBucketName { get; set; }
    public int Quantity { get; set; }
    public int? OrderId { get; set; }
    public int? SaleProductId { get; set; }
    public int? ProductHoldId { get; set; }
    public int? ProductInventoryIssueId { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
}