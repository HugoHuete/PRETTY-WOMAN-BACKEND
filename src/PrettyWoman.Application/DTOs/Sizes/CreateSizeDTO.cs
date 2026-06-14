using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Sizes;

public class CreateSizeDTO
{
    [Required(ErrorMessage = "Grupo de talla es obligatorio.")]
    public int SizeGroupId { get; set; }

    [Required(ErrorMessage = "Nombre de la talla es obligatorio.")]
    public required string Name { get; set; }

    public int DisplayOrder { get; set; }
}
