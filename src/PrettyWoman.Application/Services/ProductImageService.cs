using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PrettyWoman.Application.Services;

public class ProductImageService(
    IApplicationDbContext context,
    IMediaObjectStorage objectStorage,
    IMediaUrlResolver mediaUrlResolver) : IProductImageService
{
    private const long MaxOriginalSizeBytes = 8 * 1024 * 1024;
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public async Task<ProductImageDTO> UploadAsync(
        int productDetailId,
        Stream content,
        string? declaredContentType,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
        {
            throw new AppBadRequestException("Debe enviar una imagen válida.");
        }

        if (!string.IsNullOrWhiteSpace(declaredContentType) && !SupportedContentTypes.Contains(declaredContentType))
        {
            throw new AppUnsupportedMediaTypeException("Solo se permiten imágenes JPEG, PNG o WebP.");
        }

        if (!await context.ProductDetails.AnyAsync(product => product.Id == productDetailId, cancellationToken))
        {
            throw new AppNotFoundException($"El producto con id '{productDetailId}' no existe.");
        }

        await using var original = new MemoryStream();
        await content.CopyToAsync(original, cancellationToken);
        if (original.Length == 0 || original.Length > MaxOriginalSizeBytes)
        {
            throw new AppBadRequestException("La imagen debe tener un tamaño mayor que cero y no superar 8 MB.");
        }

        original.Position = 0;
        IImageFormat format;
        Image image;
        try
        {
            image = await Image.LoadAsync(original, cancellationToken);
            format = image.Metadata.DecodedImageFormat
                ?? throw new AppUnsupportedMediaTypeException("No se pudo identificar el formato de la imagen.");
        }
        catch (Exception exception) when (exception is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new AppUnsupportedMediaTypeException("El archivo no es una imagen JPEG, PNG o WebP válida.");
        }

        using (image)
        {
            var detectedContentType = GetContentType(format);
            if (!SupportedContentTypes.Contains(detectedContentType))
            {
                throw new AppUnsupportedMediaTypeException("Solo se permiten imágenes JPEG, PNG o WebP.");
            }

            var assetId = Guid.NewGuid();
            var baseKey = $"products/{productDetailId}/{assetId:N}";
            var originalKey = $"{baseKey}/original.{GetExtension(format)}";
            var thumbnailKey = $"{baseKey}/thumb-400.webp";
            var webKey = $"{baseKey}/web-1200.webp";
            var thumbnailUrl = mediaUrlResolver.GetPublicUrl(thumbnailKey);
            var webUrl = mediaUrlResolver.GetPublicUrl(webKey);

            await using var thumbnail = await CreateWebpAsync(image, 400, cancellationToken);
            await using var web = await CreateWebpAsync(image, 1200, cancellationToken);

            var uploaded = new List<(MediaBucket Bucket, string Key)>();
            try
            {
                original.Position = 0;
                await objectStorage.UploadAsync(MediaBucket.Private, originalKey, original, detectedContentType, cancellationToken);
                uploaded.Add((MediaBucket.Private, originalKey));
                thumbnail.Content.Position = 0;
                await objectStorage.UploadAsync(MediaBucket.Public, thumbnailKey, thumbnail.Content, "image/webp", cancellationToken);
                uploaded.Add((MediaBucket.Public, thumbnailKey));
                web.Content.Position = 0;
                await objectStorage.UploadAsync(MediaBucket.Public, webKey, web.Content, "image/webp", cancellationToken);
                uploaded.Add((MediaBucket.Public, webKey));

                var nextSortOrder = await context.ProductImages
                    .Where(productImage => productImage.ProductDetailId == productDetailId)
                    .Select(productImage => (int?)productImage.SortOrder)
                    .MaxAsync(cancellationToken) ?? -1;
                var hasPrimaryImage = await context.ProductImages
                    .AnyAsync(productImage => productImage.ProductDetailId == productDetailId && productImage.IsPrimary, cancellationToken);

                var asset = new MediaAsset
                {
                    Id = assetId,
                    StorageKey = baseKey,
                    OriginalBucket = MediaBucket.Private,
                    Visibility = MediaVisibility.Public,
                    OriginalContentType = detectedContentType,
                    OriginalSizeBytes = original.Length,
                    Width = image.Width,
                    Height = image.Height,
                    Status = MediaAssetStatus.Ready,
                    CreatedAt = DateTime.UtcNow,
                    Variants =
                    [
                        new MediaAssetVariant { Id = Guid.NewGuid(), Type = MediaVariantType.Original, Bucket = MediaBucket.Private, StorageKey = originalKey, ContentType = detectedContentType, SizeBytes = original.Length, Width = image.Width, Height = image.Height },
                        new MediaAssetVariant { Id = Guid.NewGuid(), Type = MediaVariantType.Thumbnail, Bucket = MediaBucket.Public, StorageKey = thumbnailKey, ContentType = "image/webp", SizeBytes = thumbnail.Content.Length, Width = thumbnail.Width, Height = thumbnail.Height },
                        new MediaAssetVariant { Id = Guid.NewGuid(), Type = MediaVariantType.Web, Bucket = MediaBucket.Public, StorageKey = webKey, ContentType = "image/webp", SizeBytes = web.Content.Length, Width = web.Width, Height = web.Height }
                    ]
                };
                var productImage = new ProductImage
                {
                    ProductDetailId = productDetailId,
                    MediaAsset = asset,
                    IsPrimary = !hasPrimaryImage,
                    SortOrder = nextSortOrder + 1
                };

                context.ProductImages.Add(productImage);
                await SaveProductImageAsync(productImage, cancellationToken);

                return new ProductImageDTO
                {
                    Id = productImage.Id,
                    ThumbnailUrl = thumbnailUrl,
                    WebUrl = webUrl,
                    IsPrimary = productImage.IsPrimary,
                    SortOrder = productImage.SortOrder
                };
            }
            catch
            {
                foreach (var item in uploaded)
                {
                    try { await objectStorage.DeleteAsync(item.Bucket, item.Key, CancellationToken.None); }
                    catch { /* Preserve the original error; orphan cleanup can retry later. */ }
                }
                throw;
            }
        }
    }

    public async Task<IReadOnlyCollection<ProductImageDTO>> UpdateAsync(
        int productDetailId,
        UpdateProductImagesDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ImageIdsInOrder is null)
        {
            throw new AppBadRequestException("Debe enviar la lista ordenada de imágenes.");
        }

        if (!await context.ProductDetails.AnyAsync(product => product.Id == productDetailId, cancellationToken))
        {
            throw new AppNotFoundException($"El producto con id '{productDetailId}' no existe.");
        }

        var images = await context.ProductImages
            .Where(image => image.ProductDetailId == productDetailId)
            .Include(image => image.MediaAsset)
                .ThenInclude(asset => asset!.Variants)
            .ToListAsync(cancellationToken);
        var orderedIds = request.ImageIdsInOrder;

        if (orderedIds.Count != images.Count || orderedIds.Distinct().Count() != orderedIds.Count ||
            !orderedIds.All(imageId => images.Any(image => image.Id == imageId)))
        {
            throw new AppBadRequestException("La lista debe contener exactamente una vez cada imagen del producto.");
        }

        if (!orderedIds.Contains(request.PrimaryImageId))
        {
            throw new AppBadRequestException("La imagen principal debe pertenecer a la lista enviada.");
        }

        var response = orderedIds
            .Select(imageId => MapProductImage(images.Single(image => image.Id == imageId)))
            .ToList();

        await using var transaction = await context.BeginTransactionAsync(cancellationToken);
        foreach (var image in images)
        {
            image.IsPrimary = false;
        }
        await context.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < orderedIds.Count; index++)
        {
            var image = images.Single(item => item.Id == orderedIds[index]);
            image.SortOrder = index;
            image.IsPrimary = image.Id == request.PrimaryImageId;
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        for (var index = 0; index < response.Count; index++)
        {
            response[index].IsPrimary = response[index].Id == request.PrimaryImageId;
            response[index].SortOrder = index;
        }

        return response;
    }

    public async Task DeleteAsync(int productDetailId, int imageId, CancellationToken cancellationToken = default)
    {
        var image = await context.ProductImages
            .Where(item => item.Id == imageId && item.ProductDetailId == productDetailId)
            .Include(item => item.MediaAsset)
                .ThenInclude(asset => asset!.Variants)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AppNotFoundException($"La imagen con id '{imageId}' no existe para el producto con id '{productDetailId}'.");
        var objectsToDelete = image.MediaAsset?.Variants
            .Select(variant => (variant.Bucket, variant.StorageKey))
            .ToList() ?? [];

        await using var transaction = await context.BeginTransactionAsync(cancellationToken);
        if (image.IsPrimary)
        {
            image.IsPrimary = false;
            await context.SaveChangesAsync(cancellationToken);

            var nextImage = await context.ProductImages
                .Where(item => item.ProductDetailId == productDetailId && item.Id != imageId)
                .OrderBy(item => item.SortOrder)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextImage is not null)
            {
                nextImage.IsPrimary = true;
            }
        }

        context.ProductImages.Remove(image);
        if (image.MediaAsset is not null)
        {
            context.MediaAssets.Remove(image.MediaAsset);
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        foreach (var item in objectsToDelete)
        {
            try { await objectStorage.DeleteAsync(item.Bucket, item.StorageKey, cancellationToken); }
            catch { /* The database no longer references the object; a storage cleanup can retry later. */ }
        }
    }

    private async Task SaveProductImageAsync(ProductImage productImage, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!productImage.IsPrimary || !await context.ProductImages.AnyAsync(image =>
                    image.ProductDetailId == productImage.ProductDetailId && image.IsPrimary,
                    cancellationToken))
            {
                throw;
            }

            productImage.IsPrimary = false;
            productImage.SortOrder = (await context.ProductImages
                .Where(image => image.ProductDetailId == productImage.ProductDetailId)
                .Select(image => (int?)image.SortOrder)
                .MaxAsync(cancellationToken) ?? -1) + 1;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private ProductImageDTO MapProductImage(ProductImage image)
    {
        var mediaAsset = image.MediaAsset
            ?? throw new InvalidOperationException("La imagen no tiene un recurso multimedia asociado.");
        var thumbnailKey = mediaAsset.Variants
            .FirstOrDefault(variant => variant.Type == MediaVariantType.Thumbnail)
            ?.StorageKey
            ?? throw new InvalidOperationException("La imagen no tiene variante thumbnail.");
        var webKey = mediaAsset.Variants
            .FirstOrDefault(variant => variant.Type == MediaVariantType.Web)
            ?.StorageKey
            ?? throw new InvalidOperationException("La imagen no tiene variante web.");

        return new ProductImageDTO
        {
            Id = image.Id,
            ThumbnailUrl = mediaUrlResolver.GetPublicUrl(thumbnailKey),
            WebUrl = mediaUrlResolver.GetPublicUrl(webKey),
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder
        };
    }

    private static async Task<GeneratedVariant> CreateWebpAsync(Image image, int maxWidth, CancellationToken cancellationToken)
    {
        var output = new MemoryStream();
        using var copy = image.Clone(context =>
        {
            context.AutoOrient();
            context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new SixLabors.ImageSharp.Size(maxWidth, maxWidth)
            });
        });
        await copy.SaveAsync(output, new WebpEncoder { Quality = 82 }, cancellationToken);
        output.Position = 0;
        return new GeneratedVariant(output, copy.Width, copy.Height);
    }

    private static string GetContentType(IImageFormat format) => format.Name.ToUpperInvariant() switch
    {
        "JPEG" => "image/jpeg",
        "PNG" => "image/png",
        "WEBP" => "image/webp",
        _ => "application/octet-stream"
    };

    private static string GetExtension(IImageFormat format) => format.Name.ToUpperInvariant() switch
    {
        "JPEG" => "jpg",
        "PNG" => "png",
        "WEBP" => "webp",
        _ => throw new AppUnsupportedMediaTypeException("Formato de imagen no compatible.")
    };

    private sealed record GeneratedVariant(MemoryStream Content, int Width, int Height) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Content.DisposeAsync();
    }
}
