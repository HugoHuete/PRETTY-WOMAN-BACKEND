using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Subcategories;

public class UpdateSubcategoryDTO
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Nombre de la subcategoría es obligatorio.")]
    public required string Name { get; set; }
}
