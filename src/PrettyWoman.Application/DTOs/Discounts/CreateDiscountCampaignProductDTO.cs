using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Discounts;

public class CreateDiscountCampaignProductDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "El producto del descuento es obligatorio.")]
    public int ProductDetailId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El tipo de descuento es obligatorio.")]
    public int DiscountTypeId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999", ErrorMessage = "El valor del descuento debe ser mayor que cero.")]
    public decimal DiscountValue { get; set; }
}
