using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Exceptions;
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
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO
        {
            Availability = ProductAvailabilityFilter.Unavailable,
            CategoryId = 1,
            SubcategoryId = 1,
            SizeId = 2
        });

        var productDetail = Assert.Single(result.Items);
        var product = Assert.Single(productDetail.Products);

        Assert.Equal("Pantalon cargo", productDetail.Name);
        Assert.Equal("pantalon-primary.jpg", productDetail.PrimaryImageUrl);
        Assert.Equal(2, product.SizeId);
        Assert.Equal(1, product.UnavailableQuantity);
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

        var productDetail = Assert.Single(result.Items);
        var product = Assert.Single(productDetail.Products);

        Assert.Equal("Blusa satin", productDetail.Name);
        Assert.Equal(1, product.ReservedQuantity);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCode()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO { Code = 1002 });

        var productDetail = Assert.Single(result.Items);
        Assert.Equal("Blusa satin", productDetail.Name);
        Assert.Equal(1002, productDetail.Code);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByDiscountCampaignId()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO { DiscountCampaignId = 2 });

        var productDetail = Assert.Single(result.Items);
        Assert.Equal("Blusa satin", productDetail.Name);
        Assert.All(productDetail.Products, product => Assert.Null(product.DiscountedSalePrice));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveDiscountedPrice()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);

        var result = await service.GetAllAsync(new ProductQueryDTO { Code = 1001 });

        var productDetail = Assert.Single(result.Items);
        Assert.All(productDetail.Products, product =>
        {
            Assert.Equal(650m, product.SalePrice);
            Assert.Equal(585m, product.DiscountedSalePrice);
            Assert.Equal(1, product.DiscountCampaignId);
            Assert.Equal("Promo vigente", product.DiscountCampaignName);
        });
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProductDetailWithProductsPrimaryImageAndDiscountedPrice()
    {
        await using var context = CreateContext();
        await SeedProductsAsync(context);
        var service = CreateService(context);
        var productDetailId = await context.ProductDetails
            .Where(productDetail => productDetail.Name == "Pantalon cargo")
            .Select(productDetail => productDetail.Id)
            .SingleAsync();

        var result = await service.GetByIdAsync(productDetailId);

        Assert.Equal("Pantalon cargo", result.Name);
        Assert.Equal("Pantalones", result.SubcategoryName);
        Assert.Equal("Ropa", result.CategoryName);
        Assert.Equal("pantalon-primary.jpg", result.PrimaryImageUrl);
        Assert.Equal(2, result.Products.Count);
        Assert.All(result.Products, product => Assert.Equal(585m, product.DiscountedSalePrice));
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsWhenProductDetailDoesNotExist()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.GetByIdAsync(999));

        Assert.Equal("El producto con id '999' no existe.", exception.Message);
    }

    private static ProductService CreateService(ApplicationDbContext context)
    {
        return new ProductService(context);
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

        var pants = new ProductDetail
        {
            Id = 1,
            SupplierProductCode = "PANT-001",
            Code = 1001,
            Name = "Pantalon cargo",
            SubcategoryId = 1,
            ProductImages =
            [
                new ProductImage { ImageUrl = "pantalon-secondary.jpg", SortOrder = 0, IsPrimary = false },
                new ProductImage { ImageUrl = "pantalon-primary.jpg", SortOrder = 1, IsPrimary = true }
            ],
            Products =
            [
                new Product { SizeId = 1, Quantity = 3, ReceivedQuantity = 3, AvailableQuantity = 2, SalePrice = 650m },
                new Product { SizeId = 2, Quantity = 1, ReceivedQuantity = 1, UnavailableQuantity = 1, SalePrice = 650m }
            ]
        };

        var blouse = new ProductDetail
        {
            Id = 2,
            SupplierProductCode = "BLU-001",
            Code = 1002,
            Name = "Blusa satin",
            SubcategoryId = 2,
            Products =
            [
                new Product { SizeId = 1, Quantity = 1, ReceivedQuantity = 1, ReservedQuantity = 1, SalePrice = 500m }
            ]
        };

        var shoes = new ProductDetail
        {
            Id = 3,
            SupplierProductCode = "TAC-001",
            Code = 1003,
            Name = "Tacones",
            SubcategoryId = 3,
            Products =
            [
                new Product { SizeId = 2, Quantity = 1, ReceivedQuantity = 1, UnavailableQuantity = 1, SalePrice = 900m }
            ]
        };

        context.ProductDetails.AddRange(pants, blouse, shoes);

        var now = DateTime.UtcNow;
        context.DiscountCampaigns.AddRange(
            new DiscountCampaign
            {
                Id = 1,
                Name = "Promo vigente",
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(1),
                Enabled = true,
                DiscountCampaignProducts =
                [
                    new DiscountCampaignProduct
                    {
                        ProductDetail = pants,
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
                Enabled = true,
                DiscountCampaignProducts =
                [
                    new DiscountCampaignProduct
                    {
                        ProductDetail = blouse,
                        DiscountTypeId = (int)DiscountTypeOption.FixedPrice,
                        DiscountValue = 300m
                    }
                ]
            });

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
