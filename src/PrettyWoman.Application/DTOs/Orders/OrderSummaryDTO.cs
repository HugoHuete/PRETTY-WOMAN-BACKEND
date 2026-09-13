namespace PrettyWoman.Application.DTOs.Orders;

public class OrderSummaryDTO
{
    public int Id { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderStatusId { get; set; }
    public int SupplierId { get; set; }
    public int PurchaseCurrencyId { get; set; }
    public string? PurchaseCurrencyName { get; set; }
    public decimal AmountUsd { get; set; }
    public decimal MerchandiseTotalNio { get; set; }
    public decimal ReceivedAmountNio { get; set; }
    public decimal SupplierShippingCostUsd { get; set; }
    public decimal WarehouseShippingCostUsd { get; set; }
    public decimal TotalCostNio { get; set; }
    public string? Comments { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? OrderStatusName { get; set; }
    public string? SupplierName { get; set; }
}
