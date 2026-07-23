namespace PrettyWoman.Application.DTOs.Orders;

public class OrderQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? OrderStatusId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime? PurchaseDateFrom { get; set; }
    public DateTime? PurchaseDateTo { get; set; }
}
