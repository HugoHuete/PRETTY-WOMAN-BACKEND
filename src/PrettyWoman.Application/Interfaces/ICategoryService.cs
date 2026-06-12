using PrettyWoman.Application.DTOs.Categories;

namespace PrettyWoman.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryDTO> GetByIdAsync(int id);
    Task<IEnumerable<CategoryDTO>> GetAllAsync();
    Task<int> CreateAsync(CreateCategoryDTO createCategoryDTO);
    Task UpdateAsync(int id, UpdateCategoryDTO updateCategoryDTO);
}
