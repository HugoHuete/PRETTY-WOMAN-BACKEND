using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Categories;

public class CreateCategoryDTO
{
    [Required]
    public required string Name { get; set; }
}
