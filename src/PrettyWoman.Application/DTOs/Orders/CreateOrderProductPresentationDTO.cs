using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Orders;

public class CreateOrderProductPresentationDTO
{
    public string? Name { get; set; }
    public int SortOrder { get; set; }

    [MinLength(1, ErrorMessage = "Debe enviar al menos una talla por presentación.")]
    public ICollection<CreateOrderProductVariantDTO> Sizes { get; set; } = [];
}
