using PrettyWoman.Application.DTOs.Products;

namespace PrettyWoman.Application.Interfaces;

public interface IProductImageService
{
    Task<ProductImageDTO> UploadAsync(int productDetailId, Stream content, string? declaredContentType, CancellationToken cancellationToken = default);
}
