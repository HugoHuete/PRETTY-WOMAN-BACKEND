namespace PrettyWoman.Domain.Entities;

public class ProductReceipt : IAuditableEntity
{
    public int Id { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public ICollection<OrderTrackingNumber> OrderTrackingNumbers = [];
    public ICollection<ProductReceiptDetail> ProductReceiptDetails = [];
}