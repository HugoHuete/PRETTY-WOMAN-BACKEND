using PrettyWoman.Application.DTOs.Products;

namespace PrettyWoman.Application.Interfaces;

public interface IProductImageService
{
    Task<ProductImageDTO> GetByIdAsync(int productDetailId, int imageId, CancellationToken cancellationToken = default);
    Task<ProductImageDTO> UploadAsync(int productDetailId, Stream content, string? declaredContentType, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductImageDTO>> UpdateAsync(int productDetailId, UpdateProductImagesDTO request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int productDetailId, int imageId, CancellationToken cancellationToken = default);
}
