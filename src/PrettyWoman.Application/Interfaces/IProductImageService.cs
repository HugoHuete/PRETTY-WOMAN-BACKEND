using PrettyWoman.Application.DTOs.Products;

namespace PrettyWoman.Application.Interfaces;

public interface IProductImageService
{
    Task<ProductImageDTO> GetByIdAsync(int productId, int imageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductImageDTO>> GetAllAsync(int productId, int? productPresentationId, CancellationToken cancellationToken = default);
    Task<ProductImageDTO> UploadAsync(int productId, int? productPresentationId, Stream content, string? declaredContentType, bool? isPrimary = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductImageDTO>> UpdateAsync(int productId, UpdateProductImagesDTO request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int productId, int imageId, CancellationToken cancellationToken = default);
}
