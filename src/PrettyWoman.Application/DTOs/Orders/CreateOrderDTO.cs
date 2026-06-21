using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class CreateOrderDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Proveedor es obligatorio.")]
    public int SupplierId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo de envio del proveedor a bodega en dolares debe ser mayor o igual a cero.")]
    public decimal SupplierShippingCostUsd { get; set; }

    public string? Comments { get; set; }

    [Range(1, 2, ErrorMessage = "La moneda de compra debe ser USD o NIO.")]
    public int PurchaseCurrencyId { get; set; } = 1;

    [MinLength(1, ErrorMessage = "Debe enviar al menos un producto.")]
    public ICollection<CreateOrderProductDetailDTO> ProductDetails { get; set; } = [];
}
