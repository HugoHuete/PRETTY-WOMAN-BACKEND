using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class CreateOrderDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Proveedor es obligatorio.")]
    public int SupplierId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto USD debe ser mayor o igual a cero.")]
    public decimal AmountUsd { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El total de mercaderia en cordobas debe ser mayor o igual a cero.")]
    public decimal MerchandiseTotalNio { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto recibido en cordobas debe ser mayor o igual a cero.")]
    public decimal ReceivedAmountNio { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo de envio en cordobas debe ser mayor o igual a cero.")]
    public decimal ShippingCostNio { get; set; }

    public string? Comments { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335", ErrorMessage = "La tasa de cambio debe ser mayor que cero.")]
    public decimal ExchangeRate { get; set; }
}
