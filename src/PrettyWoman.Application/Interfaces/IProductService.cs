using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products;

namespace PrettyWoman.Application.Interfaces;

public interface IProductService
{
    Task<PaginatedResult<ProductDTO>> GetAllAsync(ProductQueryDTO query);
    Task<byte[]> ExportAsync(ProductQueryDTO query);
    Task<ProductDTO> GetByIdAsync(int id);
    Task UpdatePriceAsync(int productId, int productVariantId, UpdateProductPriceDTO request);
    Task UpdatePriceForProductAsync(int productId, UpdateProductPriceDTO request);
    Task<IReadOnlyList<ProductColorDTO>> GetColorsAsync(CancellationToken cancellationToken);
    Task UpdatePresentationColorAsync(int productId, int presentationId, int? colorId, CancellationToken cancellationToken);
    Task UpdatePriceForPresentationAsync(int productId, int productPresentationId, UpdateProductPriceDTO request);
    Task<IEnumerable<ProductInventoryMovementDTO>> GetInventoryMovementsAsync(int productId, int? productVariantId = null);
}
