using PrettyWoman.Application.DTOs.ExpenseCategories;

namespace PrettyWoman.Application.Interfaces;

public interface IExpenseCategoryService
{
    Task<ExpenseCategoryDTO> GetByIdAsync(int id);
    Task<IEnumerable<ExpenseCategoryDTO>> GetAllAsync();
    Task<int> CreateAsync(CreateExpenseCategoryDTO createExpenseCategoryDTO);
    Task UpdateAsync(int id, UpdateExpenseCategoryDTO updateExpenseCategoryDTO);
}
