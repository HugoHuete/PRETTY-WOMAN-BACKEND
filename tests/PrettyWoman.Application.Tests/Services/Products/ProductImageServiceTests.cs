using Microsoft.EntityFrameworkCore;
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

    private static ProductImageService CreateService(ApplicationDbContext context, TrackingMediaObjectStorage storage) =>
        new(context, storage, new TestMediaUrlResolver());

    private static ApplicationDbContext CreateContextWithProduct()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        context.Products.Add(new Product
        {
            Id = 1,
            SupplierProductCode = "TEST-001",
            Code = 1,
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

    private sealed class TestMediaUrlResolver : IMediaUrlResolver
    {
        public string GetPublicUrl(string storageKey) => storageKey;
    }
}
