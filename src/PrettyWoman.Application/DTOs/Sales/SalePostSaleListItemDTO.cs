namespace PrettyWoman.Application.DTOs.Sales;

public class SalePostSaleListItemDTO
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public bool IsExchange { get; set; }
    public int StatusId { get; set; }
    public bool PendingReceipt { get; set; }
    public string? ClientName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SalePostSaleQueryDTO
{
    public int Page { get; set; } = 1;
    public bool History { get; set; }
    public bool IncludeAll { get; set; }
    public bool? IsExchange { get; set; }
    public int? StatusId { get; set; }
    public string? Search { get; set; }
}
