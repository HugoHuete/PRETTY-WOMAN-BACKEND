using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Inventory;

public class InventoryCatalogServiceTests
{
    [Fact]
    public async Task GetAdjustmentReasonsAsync_ReturnsReasonsOrderedById()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new InventoryCatalogService(context);

        var result = (await service.GetAdjustmentReasonsAsync()).ToList();

        Assert.Equal(
            [
                (int)InventoryAdjustmentReasonOption.ManualCorrection,
                (int)InventoryAdjustmentReasonOption.ProductCodeMixUp,
                (int)InventoryAdjustmentReasonOption.PurchaseSurplus
            ],
            result.Select(reason => reason.Id));
        Assert.Equal(nameof(InventoryAdjustmentReasonOption.ManualCorrection), result[0].Name);
    }

    [Fact]
    public async Task GetStockBucketsAsync_ReturnsBucketsOrderedById()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new InventoryCatalogService(context);

        var result = (await service.GetStockBucketsAsync()).ToList();

        Assert.Equal(
            [
                (int)InventoryStockBucketOption.External,
                (int)InventoryStockBucketOption.Available,
                (int)InventoryStockBucketOption.OutOfInventory
            ],
            result.Select(bucket => bucket.Id));
        Assert.Equal(nameof(InventoryStockBucketOption.External), result[0].Name);
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        context.InventoryAdjustmentReasons.AddRange(
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.PurchaseSurplus, Name = nameof(InventoryAdjustmentReasonOption.PurchaseSurplus) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.ManualCorrection, Name = nameof(InventoryAdjustmentReasonOption.ManualCorrection) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.ProductCodeMixUp, Name = nameof(InventoryAdjustmentReasonOption.ProductCodeMixUp) });

        context.InventoryStockBuckets.AddRange(
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Available, Name = nameof(InventoryStockBucketOption.Available) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.OutOfInventory, Name = nameof(InventoryStockBucketOption.OutOfInventory) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.External, Name = nameof(InventoryStockBucketOption.External) });

        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
