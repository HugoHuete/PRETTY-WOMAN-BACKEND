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

    [Range(0, double.MaxValue, ErrorMessage = "El monto USD debe ser mayor o igual a cero.")]
    public decimal AmountUsd { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El total de mercaderia en cordobas debe ser mayor o igual a cero.")]
    public decimal MerchandiseTotalNio { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto recibido en cordobas debe ser mayor o igual a cero.")]
    public decimal ReceivedAmountNio { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo de envio del proveedor a bodega en dolares debe ser mayor o igual a cero.")]
    public decimal SupplierShippingCostUsd { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo de envio de bodega a Nicaragua en dolares debe ser mayor o igual a cero.")]
    public decimal WarehouseShippingCostUsd { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo total en cordobas debe ser mayor o igual a cero.")]
    public decimal TotalCostNio { get; set; }

    public string? Comments { get; set; }

    [Range(typeof(decimal), "0.01", "1000", ErrorMessage = "La tasa de cambio debe ser mayor que cero.")]
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
