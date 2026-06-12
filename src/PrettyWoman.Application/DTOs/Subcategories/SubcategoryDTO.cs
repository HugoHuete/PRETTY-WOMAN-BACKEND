using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.Subcategories;

public class SubcategoryDTO
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? CategoryName { get; set; }
}
