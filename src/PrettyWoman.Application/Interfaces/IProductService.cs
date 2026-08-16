using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products;

namespace PrettyWoman.Application.Interfaces;

public interface IProductService
{
    Task<PaginatedResult<ProductDTO>> GetAllAsync(ProductQueryDTO query);
    Task<ProductDTO> GetByIdAsync(int id);
    Task UpdatePriceAsync(int productId, int productVariantId, UpdateProductPriceDTO request);
    Task<IEnumerable<ProductInventoryMovementDTO>> GetInventoryMovementsAsync(int productId, int? productVariantId = null);
}
