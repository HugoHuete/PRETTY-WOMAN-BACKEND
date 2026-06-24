using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products;

namespace PrettyWoman.Application.Interfaces;

public interface IProductService
{
    Task<PaginatedResult<ProductDetailDTO>> GetAllAsync(ProductQueryDTO query);
    Task<ProductDetailDTO> GetByIdAsync(int id);
}
