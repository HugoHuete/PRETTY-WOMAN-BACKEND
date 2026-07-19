using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.InventoryAdjustments;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Inventory;

public class InventoryAdjustmentServiceTests
{
    [Fact]
    public async Task CreateAsync_IncreasesInventoryAndLinksMovement()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var adjustmentDate = new DateTime(2026, 7, 18, 9, 0, 0, DateTimeKind.Utc);

        var id = await service.CreateAsync(new CreateInventoryAdjustmentDTO
        {
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.PurchaseSurplus,
            AdjustmentDate = adjustmentDate,
            Reference = "REC-001",
            Items =
            [
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = 1,
                    FromStockBucketId = (int)InventoryStockBucketOption.External,
                    ToStockBucketId = (int)InventoryStockBucketOption.Available,
                    Quantity = 2,
                    Comments = "Sobrante dentro de la compra."
                }
            ]
        });

        var adjustment = await context.InventoryAdjustments
            .Include(item => item.Items)
                .ThenInclude(item => item.InventoryMovement)
            .SingleAsync(item => item.Id == id);
        var product = await context.Products.SingleAsync(item => item.Id == 1);
        var movement = await context.InventoryMovements.SingleAsync();
        var adjustmentItem = Assert.Single(adjustment.Items);

        Assert.Equal(4, product.ReceivedQuantity);
        Assert.Equal(4, product.AvailableQuantity);
        Assert.Equal((int)InventoryMovementTypeOption.AdjustmentTransfer, movement.InventoryMovementTypeId);
        Assert.Equal((int)InventoryStockBucketOption.External, movement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Available, movement.ToStockBucketId);
        Assert.Equal(adjustmentItem.Id, movement.InventoryAdjustmentItemId);
        Assert.Equal(movement.Id, adjustmentItem.InventoryMovement!.Id);
        Assert.Equal("REC-001", adjustment.Reference);
        Assert.Equal(adjustmentDate, movement.MovementDate);
    }

    [Fact]
    public async Task CreateAsync_AdjustsBothProductsForCodeMixUp()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);

        await service.CreateAsync(new CreateInventoryAdjustmentDTO
        {
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.ProductCodeMixUp,
            Items =
            [
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = 3,
                    FromStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                    ToStockBucketId = (int)InventoryStockBucketOption.Available,
                    Quantity = 1
                },
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = 2,
                    FromStockBucketId = (int)InventoryStockBucketOption.Available,
                    ToStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                    Quantity = 1
                }
            ]
        });

        var correctProduct = await context.Products.SingleAsync(item => item.Id == 2);
        var wrongProduct = await context.Products.SingleAsync(item => item.Id == 3);
        var movements = await context.InventoryMovements.OrderBy(item => item.Id).ToListAsync();

        Assert.Equal(0, correctProduct.AvailableQuantity);
        Assert.Equal(1, wrongProduct.AvailableQuantity);
        Assert.Equal((int)InventoryMovementTypeOption.AdjustmentTransfer, movements[0].InventoryMovementTypeId);
        Assert.Equal((int)InventoryMovementTypeOption.AdjustmentTransfer, movements[1].InventoryMovementTypeId);
        Assert.All(movements, movement => Assert.NotNull(movement.InventoryAdjustmentItemId));
    }

    [Fact]
    public async Task CreateAsync_RejectsPurchaseSurplusAsManualAdjustment()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateInventoryAdjustmentDTO
        {
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.PurchaseSurplus,
            Items =
            [
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = 2,
                    FromStockBucketId = (int)InventoryStockBucketOption.External,
                    ToStockBucketId = (int)InventoryStockBucketOption.Available,
                    Quantity = 1
                }
            ]
        }));

        Assert.Contains("sobrantes de compra", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByProductAndReason()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = CreateService(context);
        await service.CreateAsync(new CreateInventoryAdjustmentDTO
        {
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.FoundItem,
            Items =
            [
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = 3,
                    FromStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                    ToStockBucketId = (int)InventoryStockBucketOption.Available,
                    Quantity = 1
                }
            ]
        });
        await service.CreateAsync(new CreateInventoryAdjustmentDTO
        {
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.ManualCorrection,
            Items =
            [
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = 1,
                    FromStockBucketId = (int)InventoryStockBucketOption.Available,
                    ToStockBucketId = (int)InventoryStockBucketOption.Unavailable,
                    Quantity = 1
                }
            ]
        });

        var result = await service.GetAllAsync(new InventoryAdjustmentQueryDTO
        {
            ProductId = 3,
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.FoundItem
        });

        var adjustment = Assert.Single(result.Items);
        Assert.Equal((int)InventoryAdjustmentReasonOption.FoundItem, adjustment.InventoryAdjustmentReasonId);
        Assert.Equal(3, Assert.Single(adjustment.Items).ProductId);
    }

    private static InventoryAdjustmentService CreateService(ApplicationDbContext context)
    {
        return new InventoryAdjustmentService(context, new InventoryService(context));
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        var sizeGroup = new SizeGroup { Id = 1, Name = "Regular" };
        var smallSize = new Size { Id = 1, Name = "S", SizeGroupId = 1, SizeGroup = sizeGroup, DisplayOrder = 1 };
        context.SizeGroups.Add(sizeGroup);
        context.Sizes.Add(smallSize);

        var productDetail = new ProductDetail
        {
            Id = 1,
            SupplierProductCode = "BLA-001",
            Code = 1001,
            Name = "Blazer",
            SubcategoryId = 1,
            Products =
            [
                new Product { Id = 1, SizeId = 1, Size = smallSize, Quantity = 5, ReceivedQuantity = 2, AvailableQuantity = 2, SalePrice = 800m },
                new Product { Id = 2, SizeId = 1, Size = smallSize, Quantity = 1, ReceivedQuantity = 1, AvailableQuantity = 1, SalePrice = 800m },
                new Product { Id = 3, SizeId = 1, Size = smallSize, Quantity = 1, ReceivedQuantity = 1, AvailableQuantity = 0, SalePrice = 800m }
            ]
        };
        context.ProductDetails.Add(productDetail);

        context.InventoryAdjustmentReasons.AddRange(
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.ManualCorrection, Name = nameof(InventoryAdjustmentReasonOption.ManualCorrection) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.ProductCodeMixUp, Name = nameof(InventoryAdjustmentReasonOption.ProductCodeMixUp) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.PurchaseSurplus, Name = nameof(InventoryAdjustmentReasonOption.PurchaseSurplus) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.FoundItem, Name = nameof(InventoryAdjustmentReasonOption.FoundItem) });
        context.InventoryMovementTypes.AddRange(
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.AdjustmentTransfer, Name = nameof(InventoryMovementTypeOption.AdjustmentTransfer) });
        context.InventoryStockBuckets.AddRange(
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.External, Name = nameof(InventoryStockBucketOption.External) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Available, Name = nameof(InventoryStockBucketOption.Available) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Unavailable, Name = nameof(InventoryStockBucketOption.Unavailable) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.OutOfInventory, Name = nameof(InventoryStockBucketOption.OutOfInventory) });

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
