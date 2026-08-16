using PrettyWoman.Application.DTOs.Products;

namespace PrettyWoman.Application.Interfaces;

public interface IProductImageService
{
    Task<ProductImageDTO> GetByIdAsync(int productId, int imageId, CancellationToken cancellationToken = default);
    Task<ProductImageDTO> UploadAsync(int productId, Stream content, string? declaredContentType, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductImageDTO>> UpdateAsync(int productId, UpdateProductImagesDTO request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int productId, int imageId, CancellationToken cancellationToken = default);
}
