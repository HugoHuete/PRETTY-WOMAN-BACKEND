using System.ComponentModel.DataAnnotations;

namespace PrettyWoman.Application.DTOs.ExpenseCategories;

public class CreateExpenseCategoryDTO
{
    [Required(ErrorMessage = "Nombre de la categoría de gasto es obligatorio.")]
    public required string Name { get; set; }

    public bool Enabled { get; set; } = true;
}
