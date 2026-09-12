using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Products;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAllAsync_FiltersUnavailableProductsByCategorySubcategoryAndSize()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        context.ChangeTracker.Clear();
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO
        {
            Availability = ProductAvailabilityFilter.Unavailable,
            CategoryId = 1,
            SubcategoryId = 1,
            SizeId = 2
        });

        var product = Assert.Single(result.Items);
        var presentation = Assert.Single(product.Presentations);
        var productVariant = Assert.Single(presentation.Sizes);

        Assert.Equal("Pantalon cargo", product.Name);
        Assert.Equal("pantalon-primary.jpg", product.PrimaryImageUrl);
        Assert.Equal(2, productVariant.SizeId);
        Assert.Equal("Negro", presentation.Name);
        Assert.Equal(1, productVariant.UnavailableQuantity);
    }

    [Fact]
    public async Task GetAllAsync_FiltersReservedProducts()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO
        {
            Availability = ProductAvailabilityFilter.Reserved
        });

        var product = Assert.Single(result.Items);
        var productVariant = Assert.Single(product.Presentations.Single().Sizes);

        Assert.Equal("Blusa satin", product.Name);
        Assert.Equal(1, productVariant.ReservedQuantity);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCode()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO { Code = 1002 });

        var product = Assert.Single(result.Items);
        Assert.Equal("Blusa satin", product.Name);
        Assert.Equal(1002, product.Code);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByDiscountCampaignId()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO { DiscountCampaignId = 2 });

        var product = Assert.Single(result.Items);
        Assert.Equal("Blusa satin", product.Name);
        Assert.All(product.Presentations.SelectMany(presentation => presentation.Sizes), productVariant => Assert.Null(productVariant.DiscountedSalePrice));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveDiscountedPrice()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO { Code = 1001 });

        var product = Assert.Single(result.Items);
        Assert.All(product.Presentations.SelectMany(presentation => presentation.Sizes), productVariant =>
        {
            Assert.Equal(650m, productVariant.SalePrice);
            Assert.Equal(585m, productVariant.DiscountedSalePrice);
            Assert.Equal(1, productVariant.DiscountCampaignId);
            Assert.Equal("Promo vigente", productVariant.DiscountCampaignName);
        });
    }

    [Fact]
    public async Task GetAllAsync_UsesBestDiscountBetweenProductAndSpecificVariantRules()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var variant = await context.ProductVariants.SingleAsync(productVariant => productVariant.Id == 1);
        var now = DateTime.UtcNow;
        context.DiscountCampaigns.Add(new DiscountCampaign
        {
            Id = 4,
            Name = "Promo variante específica",
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(1),
            DiscountCampaignProducts =
            [
                new DiscountCampaignProduct
                {
                    ProductVariantId = variant.Id,
                    DiscountTypeId = (int)DiscountTypeOption.FixedPrice,
                    DiscountValue = 400m
                }
            ]
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO { Code = 1001 });

        var specificVariant = result.Items.Single().Presentations.SelectMany(presentation => presentation.Sizes).Single(productVariant => productVariant.Id == variant.Id);
        var otherVariant = result.Items.Single().Presentations.SelectMany(presentation => presentation.Sizes).Single(productVariant => productVariant.Id != variant.Id);
        Assert.Equal(400m, specificVariant.DiscountedSalePrice);
        Assert.Equal(585m, otherVariant.DiscountedSalePrice);
        Assert.Equal(4, specificVariant.DiscountCampaignId);
    }

    [Fact]
    public async Task ExportAsync_ReturnsExcelWithFilteredProductVariants()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        context.ChangeTracker.Clear();
        var service = CreateService(context);

        var file = await service.ExportAsync(new ProductQueryDTO { Code = 1001 });

        using var workbook = new XLWorkbook(new MemoryStream(file));
        var worksheet = workbook.Worksheets.Single();
        var rows = worksheet.RowsUsed().ToList();

        Assert.Equal("Productos", worksheet.Name);
        Assert.Equal("Producto", worksheet.Cell(1, 1).GetString());
        Assert.Equal("Variante", worksheet.Cell(1, 5).GetString());
        Assert.Equal("Pantalon cargo", worksheet.Cell(2, 1).GetString());
        Assert.Equal("Negro", worksheet.Cell(2, 5).GetString());
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public async Task ExportAsync_AppliesVariantFiltersBeforeWritingRows()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var file = await service.ExportAsync(new ProductQueryDTO { Code = 1001, SizeId = 2 });

        using var workbook = new XLWorkbook(new MemoryStream(file));
        var worksheet = workbook.Worksheets.Single();
        var rows = worksheet.RowsUsed().ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("M", worksheet.Cell(2, 4).GetString());
    }

    [Fact]
    public async Task GetAllAsync_IgnoresCancelledAndOutOfRangeCampaigns()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var activeProduct = Assert.Single((await service.GetAllAsync(new ProductQueryDTO { Code = 1001 })).Items);
        var futureProduct = Assert.Single((await service.GetAllAsync(new ProductQueryDTO { Code = 1002 })).Items);

        Assert.All(activeProduct.Presentations.SelectMany(presentation => presentation.Sizes), productVariant =>
        {
            Assert.Equal(585m, productVariant.DiscountedSalePrice);
            Assert.Equal(1, productVariant.DiscountCampaignId);
        });
        Assert.All(futureProduct.Presentations.SelectMany(presentation => presentation.Sizes), productVariant =>
        {
            Assert.Null(productVariant.DiscountedSalePrice);
            Assert.Null(productVariant.DiscountCampaignId);
        });
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProductWithProductsPrimaryImageAndDiscountedPrice()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var productId = await context.Products
            .Where(product => product.Name == "Pantalon cargo")
            .Select(product => product.Id)
            .SingleAsync();
        context.ChangeTracker.Clear();
        var service = CreateService(context);

        var result = await service.GetByIdAsync(productId);

        Assert.Equal("Pantalon cargo", result.Name);
        Assert.Equal("Pantalones", result.SubcategoryName);
        Assert.Equal("Ropa", result.CategoryName);
        Assert.Equal("pantalon-primary.jpg", result.PrimaryImageUrl);
        var presentation = Assert.Single(result.Presentations);
        Assert.Equal("Negro", presentation.Name);
        Assert.Equal(2, presentation.Sizes.Count);
        Assert.All(presentation.Sizes, productVariant => Assert.Equal(585m, productVariant.DiscountedSalePrice));
    }

    [Fact]
    public async Task GetAllAsync_UsesGeneralImageWhenPresentationHasNoPrimaryImage()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var blackPresentation = new ProductPresentation
        {
            Name = "Negro",
            NormalizedName = "NEGRO",
            SortOrder = 0
        };
        var redPresentation = new ProductPresentation
        {
            Name = "Rojo",
            NormalizedName = "ROJO",
            SortOrder = 1
        };
        var product = new Product
        {
            Id = 4,
            SupplierProductCode = "DRESS-004",
            Code = 1004,
            Name = "Vestido colores",
            SubcategoryId = 1,
            ProductImages =
            [
                new ProductImage { IsPrimary = true, SortOrder = 0, MediaAsset = CreateMediaAsset("vestido-general.jpg") },
                new ProductImage { ProductPresentation = redPresentation, IsPrimary = true, SortOrder = 0, MediaAsset = CreateMediaAsset("rojo-primary.jpg") }
            ],
            ProductPresentations = [blackPresentation, redPresentation],
            ProductVariants =
            [
                new ProductVariant { ProductPresentation = blackPresentation, SizeId = 1, Quantity = 1, ReceivedQuantity = 1, AvailableQuantity = 1, SalePrice = 650m },
                new ProductVariant { ProductPresentation = redPresentation, SizeId = 1, Quantity = 1, ReceivedQuantity = 1, AvailableQuantity = 1, SalePrice = 650m }
            ]
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await CreateService(context).GetAllAsync(new ProductQueryDTO { Code = 1004 });

        var mappedProduct = Assert.Single(result.Items);
        Assert.Equal("vestido-general.jpg", mappedProduct.PrimaryImageUrl);
        Assert.Equal("vestido-general.jpg", mappedProduct.Presentations.Single(item => item.Name == "Negro").PrimaryImageUrl);
        Assert.Equal("rojo-primary.jpg", mappedProduct.Presentations.Single(item => item.Name == "Rojo").PrimaryImageUrl);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsWhenProductDoesNotExist()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.GetByIdAsync(999));

        Assert.Equal("El producto con id '999' no existe.", exception.Message);
    }

    [Fact]
    public async Task UpdatePriceAsync_UpdatesOnlyTheRequestedVariant()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        await service.UpdatePriceAsync(1, 1, new UpdateProductPriceDTO { SalePrice = 750m });

        Assert.Equal(750m, await context.ProductVariants
            .Where(productVariant => productVariant.Id == 1)
            .Select(productVariant => productVariant.SalePrice)
            .SingleAsync());
        Assert.Equal(650m, await context.ProductVariants
            .Where(productVariant => productVariant.Id == 2)
            .Select(productVariant => productVariant.SalePrice)
            .SingleAsync());
    }

    [Fact]
    public async Task UpdatePriceAsync_ThrowsWhenVariantDoesNotExist()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() =>
            service.UpdatePriceAsync(1, 999, new UpdateProductPriceDTO { SalePrice = 750m }));

        Assert.Equal("La variante con id '999' no existe para el producto con id '1'.", exception.Message);
    }

    [Fact]
    public async Task UpdatePriceAsync_ThrowsWhenVariantBelongsToAnotherProduct()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() =>
            service.UpdatePriceAsync(2, 1, new UpdateProductPriceDTO { SalePrice = 750m }));

        Assert.Equal("La variante con id '1' no existe para el producto con id '2'.", exception.Message);
    }

    [Fact]
    public async Task UpdatePriceAsync_DoesNotChangeHistoricalSalePrice()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var saleProduct = new SaleProduct
        {
            ProductId = 1,
            Quantity = 1,
            UnitCostAtSale = 400m,
            OriginalUnitPrice = 500m,
            FinalUnitPrice = 500m,
            LineTotal = 500m,
            TotalCostAtSale = 400m,
            GrossProfit = 100m
        };
        context.Sales.Add(new Sale { UserId = "seller", ProductVariants = [saleProduct] });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.UpdatePriceAsync(1, 1, new UpdateProductPriceDTO { SalePrice = 750m });

        var persistedSaleProduct = await context.SaleProducts.SingleAsync();
        Assert.Equal(500m, persistedSaleProduct.OriginalUnitPrice);
        Assert.Equal(500m, persistedSaleProduct.FinalUnitPrice);
        Assert.Equal(750m, await context.ProductVariants.Where(productVariant => productVariant.Id == 1).Select(productVariant => productVariant.SalePrice).SingleAsync());
    }

    [Fact]
    public async Task GetInventoryMovementsAsync_ReturnsMovementsForAllProductVariants()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = (await service.GetInventoryMovementsAsync(1)).ToList();

        Assert.Equal(2, result.Count);

        Assert.Equal("Movimiento mas reciente", result[0].Comments);
        Assert.Equal(1, result[0].ProductId);
        Assert.Equal(2, result[0].ProductVariantId);
        Assert.Equal("Pantalon cargo", result[0].ProductName);
        Assert.Equal(1001, result[0].ProductCode);
        Assert.Equal(2, result[0].SizeId);
        Assert.Equal("M", result[0].SizeName);
        Assert.Equal((int)InventoryMovementTypeOption.AdjustmentTransfer, result[0].InventoryMovementTypeId);
        Assert.Equal("AdjustmentTransfer", result[0].InventoryMovementTypeName);
        Assert.Equal((int)InventoryStockBucketOption.Available, result[0].FromStockBucketId);
        Assert.Equal("Available", result[0].FromStockBucketName);
        Assert.Equal((int)InventoryStockBucketOption.Unavailable, result[0].ToStockBucketId);
        Assert.Equal("Unavailable", result[0].ToStockBucketName);
        Assert.Equal(1, result[0].Quantity);
        Assert.Equal(7, result[0].ProductInventoryIssueId);

        Assert.Equal("Movimiento anterior", result[1].Comments);
        Assert.Equal(1, result[1].ProductVariantId);
        Assert.Equal((int)InventoryMovementTypeOption.PurchaseReceived, result[1].InventoryMovementTypeId);
        Assert.Equal(3, result[1].Quantity);
        Assert.Equal(12, result[1].OrderId);
    }

    [Fact]
    public async Task GetInventoryMovementsAsync_FiltersByProductIdWhenProvided()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = (await service.GetInventoryMovementsAsync(1, 2)).ToList();

        var movement = Assert.Single(result);
        Assert.Equal(2, movement.ProductVariantId);
        Assert.Equal("M", movement.SizeName);
        Assert.Equal("Movimiento mas reciente", movement.Comments);
    }

    [Fact]
    public async Task GetInventoryMovementsAsync_ThrowsWhenProductIdDoesNotBelongToProduct()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.GetInventoryMovementsAsync(1, 3));

        Assert.Equal("La variante con id '3' no existe para el producto con id '1'.", exception.Message);
    }
    [Fact]
    public async Task GetInventoryMovementsAsync_ThrowsWhenProductDoesNotExist()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.GetInventoryMovementsAsync(999));

        Assert.Equal("El producto con id '999' no existe.", exception.Message);
    }

    private static ProductService CreateService(ApplicationDbContext context)
    {
        return new ProductService(context, new TestMediaUrlResolver());
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        context.Categories.AddRange(
            new Category { Id = 1, Name = "Ropa" },
            new Category { Id = 2, Name = "Zapatos" });
        context.Subcategories.AddRange(
            new Subcategory { Id = 1, CategoryId = 1, Name = "Pantalones" },
            new Subcategory { Id = 2, CategoryId = 1, Name = "Blusas" },
            new Subcategory { Id = 3, CategoryId = 2, Name = "Tacones" });
        context.SizeGroups.Add(new SizeGroup { Id = 1, Name = "Regular" });
        context.Sizes.AddRange(
            new Size { Id = 1, Name = "S", SizeGroupId = 1, DisplayOrder = 1 },
            new Size { Id = 2, Name = "M", SizeGroupId = 1, DisplayOrder = 2 });


        var blackPresentation = new ProductPresentation { Name = "Negro", NormalizedName = "NEGRO" };
        var stripedPresentation = new ProductPresentation { Name = "Rayas", NormalizedName = "RAYAS" };
        var heelPresentation = new ProductPresentation { Name = "Tacón", NormalizedName = "TACÓN" };

        var pants = new Product
        {
            Id = 1,
            SupplierProductCode = "PANT-001",
            Code = 1001,
            Name = "Pantalon cargo",
            SubcategoryId = 1,
            ProductImages =
            [
                new ProductImage { MediaAsset = CreateMediaAsset("pantalon-secondary.jpg"), SortOrder = 0, IsPrimary = false },
                new ProductImage { MediaAsset = CreateMediaAsset("pantalon-primary.jpg"), SortOrder = 1, IsPrimary = true }
            ],
            ProductPresentations = [blackPresentation],
            ProductVariants =
            [
                new ProductVariant { SizeId = 1, ProductPresentation = blackPresentation, Quantity = 3, ReceivedQuantity = 3, AvailableQuantity = 2, SalePrice = 650m },
                new ProductVariant { SizeId = 2, ProductPresentation = blackPresentation, Quantity = 1, ReceivedQuantity = 1, UnavailableQuantity = 1, SalePrice = 650m }
            ]
        };

        var blouse = new Product
        {
            Id = 2,
            SupplierProductCode = "BLU-001",
            Code = 1002,
            Name = "Blusa satin",
            SubcategoryId = 2,
            ProductPresentations = [stripedPresentation],
            ProductVariants =
            [
                new ProductVariant { SizeId = 1, ProductPresentation = stripedPresentation, Quantity = 1, ReceivedQuantity = 1, ReservedQuantity = 1, SalePrice = 500m }
            ]
        };

        var shoes = new Product
        {
            Id = 3,
            SupplierProductCode = "TAC-001",
            Code = 1003,
            Name = "Tacones",
            SubcategoryId = 3,
            ProductPresentations = [heelPresentation],
            ProductVariants =
            [
                new ProductVariant { SizeId = 2, ProductPresentation = heelPresentation, Quantity = 1, ReceivedQuantity = 1, UnavailableQuantity = 1, SalePrice = 900m }
            ]
        };

        context.Products.AddRange(pants, blouse, shoes);
        context.InventoryMovementTypes.AddRange(
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.PurchaseReceived, Name = "PurchaseReceived" },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.AdjustmentTransfer, Name = "AdjustmentTransfer" });
        context.InventoryStockBuckets.AddRange(
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.External, Name = "External" },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Available, Name = "Available" },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Unavailable, Name = "Unavailable" });
        context.InventoryMovements.AddRange(
            new InventoryMovement
            {
                ProductVariant = pants.ProductVariants.ElementAt(0),
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.PurchaseReceived,
                FromStockBucketId = (int)InventoryStockBucketOption.External,
                ToStockBucketId = (int)InventoryStockBucketOption.Available,
                Quantity = 3,
                OrderId = 12,
                MovementDate = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 7, 1, 10, 5, 0, DateTimeKind.Utc),
                Comments = "Movimiento anterior"
            },
            new InventoryMovement
            {
                ProductVariant = pants.ProductVariants.ElementAt(1),
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.AdjustmentTransfer,
                FromStockBucketId = (int)InventoryStockBucketOption.Available,
                ToStockBucketId = (int)InventoryStockBucketOption.Unavailable,
                Quantity = 1,
                ProductInventoryIssueId = 7,
                MovementDate = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 7, 2, 10, 5, 0, DateTimeKind.Utc),
                Comments = "Movimiento mas reciente"
            },
            new InventoryMovement
            {
                ProductVariant = blouse.ProductVariants.Single(),
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.PurchaseReceived,
                FromStockBucketId = (int)InventoryStockBucketOption.External,
                ToStockBucketId = (int)InventoryStockBucketOption.Available,
                Quantity = 1,
                MovementDate = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 7, 3, 10, 5, 0, DateTimeKind.Utc),
                Comments = "Movimiento de otra ficha"
            });

        var now = DateTime.UtcNow;
        context.DiscountCampaigns.AddRange(
            new DiscountCampaign
            {
                Id = 1,
                Name = "Promo vigente",
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(1),
                DiscountCampaignProducts =
                [
                    new DiscountCampaignProduct
                    {
                        Product = pants,
                        DiscountTypeId = (int)DiscountTypeOption.Percentage,
                        DiscountValue = 10m
                    }
                ]
            },
            new DiscountCampaign
            {
                Id = 2,
                Name = "Promo futura",
                StartDate = now.AddDays(1),
                EndDate = now.AddDays(2),
                DiscountCampaignProducts =
                [
                    new DiscountCampaignProduct
                    {
                        Product = blouse,
                        DiscountTypeId = (int)DiscountTypeOption.FixedPrice,
                        DiscountValue = 300m
                    }
                ]
            },
            new DiscountCampaign
            {
                Id = 3,
                Name = "Promo cancelada",
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(1),
                CancelledAt = now.AddHours(-1),
                DiscountCampaignProducts =
                [
                    new DiscountCampaignProduct
                    {
                        Product = pants,
                        DiscountTypeId = (int)DiscountTypeOption.FixedPrice,
                        DiscountValue = 1m
                    }
                ]
            });

        await context.SaveChangesAsync();
    }

    private static MediaAsset CreateMediaAsset(string webStorageKey) => new()
    {
        Id = Guid.NewGuid(),
        StorageKey = $"productVariants/test/{Guid.NewGuid():N}",
        OriginalBucket = MediaBucket.Private,
        Visibility = MediaVisibility.Public,
        OriginalContentType = "image/jpeg",
        OriginalSizeBytes = 1,
        Width = 1,
        Height = 1,
        Status = MediaAssetStatus.Ready,
        CreatedAt = DateTime.UtcNow,
        Variants =
        [
            new MediaAssetVariant
            {
                Id = Guid.NewGuid(),
                Type = MediaVariantType.Web,
                Bucket = MediaBucket.Public,
                StorageKey = webStorageKey,
                ContentType = "image/webp",
                SizeBytes = 1,
                Width = 1,
                Height = 1
            }
        ]
    };

    private sealed class TestMediaUrlResolver : IMediaUrlResolver
    {
        public string GetPublicUrl(string storageKey) => storageKey;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
