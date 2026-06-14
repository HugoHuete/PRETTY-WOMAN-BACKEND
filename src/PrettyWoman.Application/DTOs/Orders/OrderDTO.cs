using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class OrderDTO
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderStatusId { get; set; }
    public int SupplierId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto debe ser mayor o igual a cero.")]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto USD debe ser mayor o igual a cero.")]
    public decimal AmountUsd { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto recibido debe ser mayor o igual a cero.")]
    public decimal ReceivedAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo total de envío debe ser mayor o igual a cero.")]
    public decimal TotalShippingCost { get; set; }

    public string? Comments { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "La tasa de cambio debe ser mayor o igual a cero.")]
    public decimal ExchangeRate { get; set; }

    public string? OrderStatusName { get; set; }
    public string? SupplierName { get; set; }
}
