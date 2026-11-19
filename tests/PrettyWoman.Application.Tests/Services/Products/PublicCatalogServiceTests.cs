using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.PublicCatalog;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Products;

public class PublicCatalogServiceTests
{
    [Fact]
    public async Task ListAsync_SkipsProductThatSellsOutWhileLoadingPriceSortedPage()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot()).Options;
        await using var inventory = new ApplicationDbContext(options);
        var subcategory = new Subcategory { Name = "Blusas", Category = new Category { Name = "Ropa" } };
        var size = new Size { Name = "M", SizeGroup = new SizeGroup { Name = "Ropa" } };

        ProductVariant AddVariant(int code, decimal price)
        {
            var product = new Product { Code = code, Name = $"Prenda {code}", SupplierProductCode = $"SKU-{code}", Subcategory = subcategory };
            return inventory.ProductVariants.Add(new ProductVariant
            {
                Product = product,
                ProductPresentation = new ProductPresentation { Product = product },
                Size = size,
                AvailableQuantity = 1,
                SalePrice = price
            }).Entity;
        }

        var sellingOut = AddVariant(1, 90m);
        var available = AddVariant(2, 100m);
        await inventory.SaveChangesAsync();

        var interceptor = new BeforeLoadingPage(() =>
        {
            sellingOut.AvailableQuantity = 0;
            inventory.SaveChanges();
        });
        var catalogOptions = new DbContextOptionsBuilder<ApplicationDbContext>(options)
            .AddInterceptors(interceptor).Options;
        await using var context = new ApplicationDbContext(catalogOptions);

        var result = await new PublicCatalogService(context, new Resolver())
            .ListAsync(new CatalogQueryDTO(Sort: "priceAsc", PageSize: 2));

        Assert.True(interceptor.Ran);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(available.ProductId, Assert.Single(result.Items).Id);
    }

    private sealed class BeforeLoadingPage(Action updateInventory) : IQueryExpressionInterceptor
    {
        public bool Ran { get; private set; }

        public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
        {
            if (!Ran && queryExpression.ToString().Contains(".Include(product => product.ProductImages)", StringComparison.Ordinal))
            {
                Ran = true;
                updateInventory();
            }

            return queryExpression;
        }
    }

    [Fact]
    public async Task ListAsync_UsesPresentationDiscountForFilteringAndPricePagination()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var category = new Category { Name = "Ropa" };
        var subcategory = new Subcategory { Name = "Blusas", Category = category };
        var size = new Size { Name = "M", SizeGroup = new SizeGroup { Name = "Ropa" } };
        var product = new Product { Name = "Con descuento", SupplierProductCode = "A", Code = 1, Subcategory = subcategory };
        var other = new Product { Name = "Sin descuento", SupplierProductCode = "B", Code = 2, Subcategory = subcategory };
        var presentation = new ProductPresentation { Product = product };
        var otherPresentation = new ProductPresentation { Product = other };
        context.ProductVariants.AddRange(
            new ProductVariant { Product = product, ProductPresentation = presentation, Size = size, AvailableQuantity = 1, SalePrice = 100m },
            new ProductVariant { Product = other, ProductPresentation = otherPresentation, Size = size, AvailableQuantity = 1, SalePrice = 90m });
        var campaign = new DiscountCampaign { Name = "Prueba", StartDate = DateTime.UtcNow.AddHours(-1), EndDate = DateTime.UtcNow.AddHours(1) };
        context.DiscountCampaignProducts.AddRange(
            new DiscountCampaignProduct { Product = product, DiscountCampaign = campaign, DiscountTypeId = (int)DiscountTypeOption.FixedPrice, DiscountValue = 50m },
            new DiscountCampaignProduct { ProductPresentation = presentation, DiscountCampaign = campaign, DiscountTypeId = (int)DiscountTypeOption.FixedAmount, DiscountValue = 5m });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new PublicCatalogService(context, new Resolver());

        var sorted = await service.ListAsync(new CatalogQueryDTO(Sort: "priceAsc", PageSize: 1));
        var sale = await service.ListAsync(new CatalogQueryDTO(OnSale: true, Sort: "priceAsc"));

        Assert.Equal(2, sorted.TotalCount);
        Assert.Equal("Sin descuento", Assert.Single(sorted.Items).Name);
        Assert.Equal(1, sale.TotalCount);
        Assert.Equal(95m, Assert.Single(sale.Items).FinalPrice);
    }

    [Fact]
    public async Task FiltersAsync_OnlyListsCategoriesAndSubcategoriesWithAvailableProducts()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var clothing = new Category { Name = "Ropa" };
        var stocked = new Subcategory { Name = "Blusas", Category = clothing };
        var soldOut = new Subcategory { Name = "Vestidos", Category = clothing };
        var other = new Subcategory { Name = "Bolsos", Category = new Category { Name = "Accesorios" } };
        var size = new Size { Name = "M", SizeGroup = new SizeGroup { Name = "Ropa" } };

        foreach (var (subcategory, quantity) in new[] { (stocked, 2), (soldOut, 0), (other, 0) })
        {
            var product = new Product { Name = subcategory.Name, SupplierProductCode = subcategory.Name, Subcategory = subcategory };
            context.ProductVariants.Add(new ProductVariant
            {
                Product = product, ProductPresentation = new ProductPresentation { Product = product },
                Size = size, AvailableQuantity = quantity
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var filters = await new PublicCatalogService(context, new Resolver()).FiltersAsync();

        var category = Assert.Single(filters.Categories);
        Assert.Equal("Ropa", category.Name);
        Assert.Equal("Blusas", Assert.Single(category.Subcategories).Name);
    }

    [Fact]
    public async Task FiltersAsync_OrdersSizeGroupsByIdAndSizesByDisplayOrder()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var clothing = new SizeGroup { Id = 2, Name = "Ropa" };
        var shoes = new SizeGroup { Id = 5, Name = "Calzado" };
        context.Sizes.AddRange(
            new Size { Name = "M", SizeGroup = clothing, DisplayOrder = 2 },
            new Size { Name = "7", SizeGroup = shoes, DisplayOrder = 1 },
            new Size { Name = "S", SizeGroup = clothing, DisplayOrder = 1 });
        await context.SaveChangesAsync();

        var filters = await new PublicCatalogService(context, new Resolver()).FiltersAsync();

        Assert.Equal(new[] { 2, 5 }, filters.SizeGroups.Select(group => group.Id));
        Assert.Equal(new[] { "S", "M" }, filters.SizeGroups[0].Sizes.Select(size => size.Name));
        Assert.Equal("7", Assert.Single(filters.SizeGroups[1].Sizes).Name);
    }

    [Fact]
    public async Task ListAsync_ListsOnlyColorsWithAvailableMatchingVariants()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var category = new Category { Name = "Ropa" };
        var subcategory = new Subcategory { Name = "Blusas", Category = category };
        var group = new SizeGroup { Name = "Ropa" };
        var small = new Size { Name = "S", SizeGroup = group };
        var medium = new Size { Name = "M", SizeGroup = group };
        var product = new Product { Name = "Blusa", SupplierProductCode = "A", Code = 1, Subcategory = subcategory };
        var blue = new ProductPresentation { Product = product, Name = "Azul", SortOrder = 1 };
        var pink = new ProductPresentation { Product = product, Name = "Rosa", SortOrder = 2 };
        var black = new ProductPresentation { Product = product, Name = "Negro", SortOrder = 3 };

        foreach (var (color, size, stock) in new[] { (blue, small, 1), (pink, medium, 1), (black, small, 0) })
            context.ProductVariants.Add(new ProductVariant
            {
                Product = product, ProductPresentation = color, Size = size, AvailableQuantity = stock, SalePrice = 100
            });
        var blueImage = CreateImage("azul.webp", blue, false);
        var pinkImage = CreateImage("rosa.webp", pink, true);
        var generalImage = CreateImage("general.webp", null, false);
        product.ProductImages.Add(blueImage);
        product.ProductImages.Add(pinkImage);
        product.ProductImages.Add(generalImage);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new PublicCatalogService(context, new Resolver());

        var all = Assert.Single((await service.ListAsync(new())).Items);
        var filtered = Assert.Single((await service.ListAsync(new(SizeId: small.Id))).Items);

        Assert.Equal(new[] { "Azul", "Rosa" }, all.Colors.Select(color => color.Name));
        Assert.Equal("Azul", Assert.Single(filtered.Colors).Name);
        Assert.Equal("rosa.webp", all.ImageUrl);
        Assert.Equal("azul.webp", filtered.ImageUrl);

        context.ProductImages.Remove(blueImage);
        await context.SaveChangesAsync();
        Assert.Equal("general.webp", Assert.Single((await service.ListAsync(new(SizeId: small.Id))).Items).ImageUrl);

        context.ProductImages.Remove(generalImage);
        await context.SaveChangesAsync();
        Assert.Null(Assert.Single((await service.ListAsync(new(SizeId: small.Id))).Items).ImageUrl);

        static ProductImage CreateImage(string file, ProductPresentation? color, bool primary) => new()
        {
            ProductPresentation = color,
            IsPrimary = primary,
            MediaAsset = new MediaAsset
            {
                StorageKey = file,
                OriginalContentType = "image/webp",
                Visibility = MediaVisibility.Public,
                Status = MediaAssetStatus.Ready,
                Variants = [new MediaAssetVariant
                {
                    Type = MediaVariantType.Web, Bucket = MediaBucket.Public, StorageKey = file, ContentType = "image/webp"
                }]
            }
        };
    }

    private sealed class Resolver : IMediaUrlResolver
    {
        public string GetPublicUrl(string storageKey) => storageKey;
    }
}
