using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace PrettyWoman.Application.Tests.Services.Products;

public class ProductImageServiceTests
{
    [Fact]
    public async Task UploadAsync_RejectsImageLargerThanFourMegabytesBeforeWritingToStorage()
    {
        await using var context = CreateContextWithProduct();
        var storage = new TrackingMediaObjectStorage();
        var service = CreateService(context, storage);
        await using var content = new MemoryStream(new byte[(4 * 1024 * 1024) + 1]);

        await Assert.ThrowsAsync<AppBadRequestException>(() => service.UploadAsync(
            1,
            null,
            content,
            "image/jpeg"));

        Assert.Empty(storage.Uploads);
        Assert.Empty(context.ProductImages);
    }

    [Fact]
    public async Task UploadAsync_RejectsImageExceedingDimensionLimitBeforeWritingToStorage()
    {
        await using var context = CreateContextWithProduct();
        var storage = new TrackingMediaObjectStorage();
        var service = CreateService(context, storage);
        await using var content = await CreatePngAsync(width: 6001, height: 1);

        await Assert.ThrowsAsync<AppBadRequestException>(() => service.UploadAsync(
            1,
            null,
            content,
            "image/png"));

        Assert.Empty(storage.Uploads);
        Assert.Empty(context.ProductImages);
    }

    [Fact]
    public async Task UploadAsync_UsesProductCodeAndPresentationNameInStorageKeys()
    {
        await using var context = CreateContextWithProduct();
        var storage = new TrackingMediaObjectStorage();
        var service = CreateService(context, storage);
        await using var content = await CreatePngAsync(width: 1, height: 1);

        AddPresentation(context, "Tamaño / Azul");
        context.SaveChanges();

        await service.UploadAsync(1, 10, content, "image/png");

        Assert.Equal(3, storage.Uploads.Count);
        var baseKey = storage.Uploads[0].StorageKey[..^4];
        Assert.StartsWith("products/42/tamano-azul_1_", baseKey);
        Assert.Equal(
            [
                (MediaBucket.Private, $"{baseKey}.png"),
                (MediaBucket.Public, $"{baseKey}-thumb-400.webp"),
                (MediaBucket.Public, $"{baseKey}-web-1200.webp")
            ],
            storage.Uploads);
    }

    [Fact]
    public async Task UploadAsync_WhenMarkedPrimary_ReplacesExistingPrimaryImage()
    {
        await using var context = CreateContextWithProduct();
        var storage = new TrackingMediaObjectStorage();
        var service = CreateService(context, storage);
        AddPresentation(context, "Rojo");
        context.SaveChanges();

        await using var firstContent = await CreatePngAsync(width: 1, height: 1);
        var first = await service.UploadAsync(1, 10, firstContent, "image/png");
        await using var secondContent = await CreatePngAsync(width: 1, height: 1);

        var second = await service.UploadAsync(1, 10, secondContent, "image/png", isPrimary: true);

        var firstBaseKey = storage.Uploads[0].StorageKey[..^4];
        var secondBaseKey = storage.Uploads[3].StorageKey[..^4];
        Assert.NotEqual(firstBaseKey, secondBaseKey);

        var images = await context.ProductImages.OrderBy(image => image.Id).ToListAsync();
        Assert.False(images.Single(image => image.Id == first.Id).IsPrimary);
        Assert.True(images.Single(image => image.Id == second.Id).IsPrimary);
    }
    [Fact]
    public async Task UploadAsync_UsesOnlyCounterWhenPresentationHasNoName()
    {
        await using var context = CreateContextWithProduct();
        var storage = new TrackingMediaObjectStorage();
        var service = CreateService(context, storage);
        await using var content = await CreatePngAsync(width: 1, height: 1);

        AddPresentation(context, null);
        context.SaveChanges();

        await service.UploadAsync(1, 10, content, "image/png");

        Assert.Equal(3, storage.Uploads.Count);
        var baseKey = storage.Uploads[0].StorageKey[..^4];
        Assert.StartsWith("products/42/1_", baseKey);
        Assert.Equal(
            [
                (MediaBucket.Private, $"{baseKey}.png"),
                (MediaBucket.Public, $"{baseKey}-thumb-400.webp"),
                (MediaBucket.Public, $"{baseKey}-web-1200.webp")
            ],
            storage.Uploads);
    }

    [Fact]
    public async Task UploadAsync_DoesNotReuseKeysAfterDeletingHighestSortedImage()
    {
        await using var context = CreateContextWithProduct();
        var storage = new TrackingMediaObjectStorage();
        var service = CreateService(context, storage);
        AddPresentation(context, "Rojo");
        context.SaveChanges();

        await using var firstContent = await CreatePngAsync(width: 1, height: 1);
        await service.UploadAsync(1, 10, firstContent, "image/png");
        var firstBaseKey = storage.Uploads[0].StorageKey[..^4];
        var firstImageId = context.ProductImages.Single().Id;
        await service.DeleteAsync(1, firstImageId);

        await using var secondContent = await CreatePngAsync(width: 1, height: 1);
        await service.UploadAsync(1, 10, secondContent, "image/png");
        var secondBaseKey = storage.Uploads[3].StorageKey[..^4];

        Assert.NotEqual(firstBaseKey, secondBaseKey);
        Assert.StartsWith("products/42/rojo_1_", secondBaseKey);
    }

    [Fact]
    public async Task UploadAsync_PropagatesUnrelatedDatabaseUpdateException()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new ThrowingSaveChangesContext(options, new PostgresException("duplicate key value violates unique constraint other_index"));
        context.Products.Add(new Product
        {
            Id = 1,
            SupplierProductCode = "TEST-001",
            Code = 42,
            Name = "Producto de prueba",
            SubcategoryId = 1
        });
        context.SaveChanges();
        context.ThrowOnNextSave = true;

        var storage = new TrackingMediaObjectStorage();
        var service = CreateService(context, storage);
        await using var content = await CreatePngAsync(width: 1, height: 1);

        await Assert.ThrowsAsync<DbUpdateException>(() => service.UploadAsync(1, null, content, "image/png"));
    }

    [Fact]
    public async Task UploadAsync_PrimaryConflictFallbackScopesSortOrderToPresentation()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new ThrowingSaveChangesContext(options, new PostgresException("duplicate key value violates unique constraint ix_product_images_product_id_presentation_primary"));
        context.Products.Add(new Product
        {
            Id = 1,
            SupplierProductCode = "TEST-001",
            Code = 42,
            Name = "Producto de prueba",
            SubcategoryId = 1
        });
        context.ProductPresentations.Add(new ProductPresentation { Id = 10, ProductId = 1, Name = "Rojo", NormalizedName = "ROJO", SortOrder = 0 });
        context.ProductPresentations.Add(new ProductPresentation { Id = 20, ProductId = 1, Name = "Verde", NormalizedName = "VERDE", SortOrder = 1 });
        AddImage(context, 1, 10, sortOrder: 0, isPrimary: true, "red-1");
        AddImage(context, 2, 20, sortOrder: 99, isPrimary: true, "green-100");
        context.SaveChanges();
        context.ThrowOnNextSave = true;

        var service = CreateService(context, new TrackingMediaObjectStorage());
        await using var content = await CreatePngAsync(width: 1, height: 1);
        var result = await service.UploadAsync(1, 10, content, "image/png", isPrimary: true);

        Assert.Equal(1, result.SortOrder);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByPresentationAndOrdersBySortOrder()
    {
        await using var context = CreateContextWithProduct();
        AddPresentation(context, "Rojo");
        AddImage(context, 1, null, sortOrder: 0, isPrimary: true, "general");
        AddImage(context, 2, 10, sortOrder: 2, isPrimary: false, "red-2");
        AddImage(context, 3, 10, sortOrder: 0, isPrimary: true, "red-1");
        context.SaveChanges();
        var service = CreateService(context, new TrackingMediaObjectStorage());

        var result = await service.GetAllAsync(1, 10);

        Assert.Equal([3, 2], result.Select(image => image.Id));
        Assert.Equal(["red-1-thumb.webp", "red-2-thumb.webp"], result.Select(image => image.ThumbnailUrl));
        Assert.True(result.First().IsPrimary);
        Assert.False(result.Last().IsPrimary);
    }

    private static void AddImage(ApplicationDbContext context, int id, int? presentationId, int sortOrder, bool isPrimary, string key)
    {
        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            StorageKey = key,
            OriginalBucket = MediaBucket.Private,
            Visibility = MediaVisibility.Public,
            OriginalContentType = "image/png",
            OriginalSizeBytes = 1,
            Width = 1,
            Height = 1,
            Status = MediaAssetStatus.Ready,
            CreatedAt = DateTime.UtcNow,
            Variants =
            [
                new MediaAssetVariant { Id = Guid.NewGuid(), Type = MediaVariantType.Thumbnail, Bucket = MediaBucket.Public, StorageKey = $"{key}-thumb.webp", ContentType = "image/webp" },
                new MediaAssetVariant { Id = Guid.NewGuid(), Type = MediaVariantType.Web, Bucket = MediaBucket.Public, StorageKey = $"{key}-web.webp", ContentType = "image/webp" }
            ]
        };
        context.ProductImages.Add(new ProductImage
        {
            Id = id,
            ProductId = 1,
            ProductPresentationId = presentationId,
            IsPrimary = isPrimary,
            SortOrder = sortOrder,
            MediaAsset = asset
        });
    }
    private static void AddPresentation(ApplicationDbContext context, string? name) => context.ProductPresentations.Add(new ProductPresentation { Id = 10, ProductId = 1, Name = name, NormalizedName = name?.ToUpperInvariant(), SortOrder = 0 });

    private static ProductImageService CreateService(ApplicationDbContext context, TrackingMediaObjectStorage storage) =>
        new(context, storage, new TestMediaUrlResolver());

    private static ApplicationDbContext CreateContextWithProduct()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new ApplicationDbContext(options);
        context.Products.Add(new Product
        {
            Id = 1,
            SupplierProductCode = "TEST-001",
            Code = 42,
            Name = "Producto de prueba",
            SubcategoryId = 1
        });
        context.SaveChanges();
        return context;
    }

    private static async Task<MemoryStream> CreatePngAsync(int width, int height)
    {
        var stream = new MemoryStream();
        using var image = new Image<Rgba32>(width, height);
        await image.SaveAsync(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }

    private sealed class TrackingMediaObjectStorage : IMediaObjectStorage
    {
        public List<(MediaBucket Bucket, string StorageKey)> Uploads { get; } = [];

        public Task UploadAsync(MediaBucket bucket, string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            Uploads.Add((bucket, storageKey));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(MediaBucket bucket, string storageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingSaveChangesContext(DbContextOptions<ApplicationDbContext> options, Exception exception) : ApplicationDbContext(options)
    {
        public bool ThrowOnNextSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnNextSave)
            {
                ThrowOnNextSave = false;
                throw new DbUpdateException("Simulated database failure.", exception);
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class PostgresException(string message) : Exception(message);

    private sealed class TestMediaUrlResolver : IMediaUrlResolver
    {
        public string GetPublicUrl(string storageKey) => storageKey;
    }
}
