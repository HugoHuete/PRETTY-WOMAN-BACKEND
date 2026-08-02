using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class OrderDTO
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
    public decimal TotalShortageLossNio { get; set; }
    public decimal TotalSupplierRefundNio { get; set; }
    public decimal NetShortageLossNio { get; set; }
    public SupplierRefundDTO? SupplierRefund { get; set; }
    public DateTime? SupplierRefundDeclinedAt { get; set; }
    public string? SupplierRefundDeclineComments { get; set; }
    public ICollection<PurchaseShortageDTO> PurchaseShortages { get; set; } = [];
    public ICollection<OrderProductDetailDTO> ProductDetails { get; set; } = [];
}
