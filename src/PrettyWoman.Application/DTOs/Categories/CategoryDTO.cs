using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Categories;

public class CategoryDTO
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }
}
