using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Subcategories;

public class UpdateSubcategoryDTO
{
    public int CategoryId { get; set; }

    [Required]
    public required string Name { get; set; }
}
