using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.DTOs.Orders;

public class PurchaseShortageDTO
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal LossAmountNio { get; set; }
    public DateTime ShortageDate { get; set; }
    public string? Comments { get; set; }
    public PurchaseShortageRefundStatusOption RefundStatus { get; set; }
}
