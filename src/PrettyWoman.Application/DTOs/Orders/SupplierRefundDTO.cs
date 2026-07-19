namespace PrettyWoman.Application.DTOs.Orders;

public class SupplierRefundDTO
{
    public int Id { get; set; }
    public int FinancialMovementId { get; set; }
    public decimal AmountNio { get; set; }
    public DateTime RefundedAt { get; set; }
    public string? Reference { get; set; }
    public string? Comments { get; set; }
}
