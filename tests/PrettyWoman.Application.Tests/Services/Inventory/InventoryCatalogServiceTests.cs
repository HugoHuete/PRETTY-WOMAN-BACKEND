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
                (int)InventoryAdjustmentReasonOption.PurchaseSurplus,
                (int)InventoryAdjustmentReasonOption.PurchaseShortage,
                (int)InventoryAdjustmentReasonOption.LostItem,
                (int)InventoryAdjustmentReasonOption.FoundItem,
                (int)InventoryAdjustmentReasonOption.Donation,
                (int)InventoryAdjustmentReasonOption.Other
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
                (int)InventoryStockBucketOption.Reserved,
                (int)InventoryStockBucketOption.Unavailable,
                (int)InventoryStockBucketOption.OutOfInventory
            ],
            result.Select(bucket => bucket.Id));
        Assert.Equal(nameof(InventoryStockBucketOption.External), result[0].Name);
    }

    [Fact]
    public async Task GetAdjustmentReasonSuggestionsAsync_ReturnsExpectedSuggestedMovements()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new InventoryCatalogService(context);

        var result = (await service.GetAdjustmentReasonSuggestionsAsync()).ToList();

        Assert.Equal(Enum.GetValues<InventoryAdjustmentReasonOption>().Length, result.Count);

        var codeMixUp = result.Single(suggestion =>
            suggestion.InventoryAdjustmentReasonId == (int)InventoryAdjustmentReasonOption.ProductCodeMixUp);
        Assert.Equal(nameof(InventoryAdjustmentReasonOption.ProductCodeMixUp), codeMixUp.InventoryAdjustmentReasonName);
        Assert.Contains(codeMixUp.SuggestedMovements, movement =>
            movement.FromStockBucketId == (int)InventoryStockBucketOption.OutOfInventory &&
            movement.ToStockBucketId == (int)InventoryStockBucketOption.Available);
        Assert.Contains(codeMixUp.SuggestedMovements, movement =>
            movement.FromStockBucketId == (int)InventoryStockBucketOption.Available &&
            movement.ToStockBucketId == (int)InventoryStockBucketOption.OutOfInventory);

        var purchaseSurplus = result.Single(suggestion =>
            suggestion.InventoryAdjustmentReasonId == (int)InventoryAdjustmentReasonOption.PurchaseSurplus);
        Assert.Empty(purchaseSurplus.SuggestedMovements);
        Assert.Contains("cantidad comprada", purchaseSurplus.Description);
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        context.InventoryAdjustmentReasons.AddRange(
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.PurchaseSurplus, Name = nameof(InventoryAdjustmentReasonOption.PurchaseSurplus) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.ManualCorrection, Name = nameof(InventoryAdjustmentReasonOption.ManualCorrection) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.ProductCodeMixUp, Name = nameof(InventoryAdjustmentReasonOption.ProductCodeMixUp) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.PurchaseShortage, Name = nameof(InventoryAdjustmentReasonOption.PurchaseShortage) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.LostItem, Name = nameof(InventoryAdjustmentReasonOption.LostItem) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.FoundItem, Name = nameof(InventoryAdjustmentReasonOption.FoundItem) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.Donation, Name = nameof(InventoryAdjustmentReasonOption.Donation) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.Other, Name = nameof(InventoryAdjustmentReasonOption.Other) });

        context.InventoryStockBuckets.AddRange(
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Available, Name = nameof(InventoryStockBucketOption.Available) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.OutOfInventory, Name = nameof(InventoryStockBucketOption.OutOfInventory) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.External, Name = nameof(InventoryStockBucketOption.External) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Reserved, Name = nameof(InventoryStockBucketOption.Reserved) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Unavailable, Name = nameof(InventoryStockBucketOption.Unavailable) });

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
