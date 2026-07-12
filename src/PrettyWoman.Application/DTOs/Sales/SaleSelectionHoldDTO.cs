namespace PrettyWoman.Application.DTOs.Sales;

public class SaleSelectionHoldDTO
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int ProductHoldStatusId { get; set; }
    public string? ProductHoldStatusName { get; set; }
    public DateTime HoldDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Comments { get; set; }
}
