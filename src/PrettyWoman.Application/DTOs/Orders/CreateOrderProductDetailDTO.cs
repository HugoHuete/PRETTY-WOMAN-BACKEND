using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class CreateOrderProductDetailDTO
{
    public int? Id { get; set; } // Used when updating orders

    [Required(ErrorMessage = "Código de proveedor es obligatorio.")]
    public required string SupplierProductCode { get; set; }

    [Required(ErrorMessage = "Nombre es obligatorio.")]
    public required string Name { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Subcategoría es obligatoria.")]
    public int SubcategoryId { get; set; }

    [MinLength(1, ErrorMessage = "Debe enviar al menos una variante del producto.")]
    public ICollection<CreateOrderProductVariantDTO> Variants { get; set; } = [];
}
