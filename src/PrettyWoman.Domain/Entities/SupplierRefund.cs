namespace PrettyWoman.Domain.Entities;

public class SupplierRefund : IAuditableEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int FinancialMovementId { get; set; }
    public decimal AmountNio { get; set; }
    public DateTime RefundedAt { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }

    public Order? Order { get; set; }
    public FinancialMovement? FinancialMovement { get; set; }
}
