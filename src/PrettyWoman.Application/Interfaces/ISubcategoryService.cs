using PrettyWoman.Application.DTOs.Subcategories;

namespace PrettyWoman.Application.Interfaces;

public interface ISubcategoryService
{
    Task<SubcategoryDTO> GetByIdAsync(int id);
    Task<IEnumerable<SubcategoryDTO>> GetAllAsync(int? categoryId = null);
    Task<int> CreateAsync(CreateSubcategoryDTO createSubcategoryDTO);
    Task UpdateAsync(int id, UpdateSubcategoryDTO updateSubcategoryDTO);
}
