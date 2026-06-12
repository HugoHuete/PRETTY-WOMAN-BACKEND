using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.ExpenseCategories;

public class CreateExpenseCategoryDTO
{
    [Required]
    public required string Name { get; set; }

    public bool Enabled { get; set; } = true;
}
