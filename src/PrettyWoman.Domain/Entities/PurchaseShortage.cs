namespace PrettyWoman.Domain.Entities;

public class PurchaseShortage : IAuditableEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal LossAmountNio { get; set; }
    public DateTime ShortageDate { get; set; }
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
}
