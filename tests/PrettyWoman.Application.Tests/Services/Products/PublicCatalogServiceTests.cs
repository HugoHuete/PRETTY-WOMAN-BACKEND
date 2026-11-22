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
            var product = new Product { IsPublishedInCatalog = true, Code = code, Name = $"Prenda {code}", SupplierProductCode = $"SKU-{code}", Subcategory = subcategory };
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

    [Theory]
    [InlineData("newest")]
    [InlineData("priceAsc")]
    public async Task ListAsync_MatchesAnyValueWithinEachActiveFilter(string sort)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var clothing = new Category { Name = "Ropa" };
        var shoes = new Category { Name = "Calzado" };
        var other = new Category { Name = "Accesorios" };
        var blouses = new Subcategory { Name = "Blusas", Category = clothing };
        var sandals = new Subcategory { Name = "Sandalias", Category = shoes };
        var bags = new Subcategory { Name = "Bolsos", Category = other };
        var dresses = new Subcategory { Name = "Vestidos", Category = clothing };
        var group = new SizeGroup { Name = "Tallas" };
        var small = new Size { Name = "S", SizeGroup = group };
        var medium = new Size { Name = "M", SizeGroup = group };
        var large = new Size { Name = "L", SizeGroup = group };

        AddVariant(1, blouses, small, 10);
        AddVariant(2, sandals, medium, 20);
        AddVariant(3, bags, small, 30);
        AddVariant(4, dresses, small, 40);
        AddVariant(5, blouses, large, 5);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new PublicCatalogService(context, new Resolver()).ListAsync(new(
            Sort: sort, CategoryIds: [clothing.Id, shoes.Id],
            SubcategoryIds: [blouses.Id, sandals.Id, bags.Id], SizeIds: [small.Id, medium.Id]));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { 1, 2 }, result.Items.Select(item => item.Code).OrderBy(code => code));
        Assert.Equal(new[] { "S", "M" }, result.Items.OrderBy(item => item.Code).Select(item => Assert.Single(item.Sizes)));

        var mixed = await new PublicCatalogService(context, new Resolver()).ListAsync(new(
            Sort: sort, CategoryIds: [clothing.Id], SubcategoryIds: [sandals.Id],
            SizeIds: [small.Id, medium.Id], MatchAnyCategory: true));

        Assert.Equal(3, mixed.TotalCount);
        Assert.Equal(new[] { 1, 2, 4 }, mixed.Items.Select(item => item.Code).OrderBy(code => code));

        void AddVariant(int code, Subcategory subcategory, Size size, decimal price)
        {
            var product = new Product
            {
                Code = code,
                IsPublishedInCatalog = true,
                Name = $"Prenda {code}",
                SupplierProductCode = $"SKU-{code}",
                Subcategory = subcategory
            };
            context.ProductVariants.Add(new ProductVariant
            {
                Product = product,
                ProductPresentation = new ProductPresentation { Product = product },
                Size = size,
                AvailableQuantity = 1,
                SalePrice = price
            });
        }
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
        var product = new Product { IsPublishedInCatalog = true, Name = "Con descuento", SupplierProductCode = "A", Code = 1, Subcategory = subcategory };
        var other = new Product { IsPublishedInCatalog = true, Name = "Sin descuento", SupplierProductCode = "B", Code = 2, Subcategory = subcategory };
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
    public async Task Catalog_ExcludesUnpublishedProductsFromListDetailAndCategories()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var clothing = new Category { Name = "Ropa" };
        var stocked = new Subcategory { Name = "Blusas", Category = clothing };
        var soldOut = new Subcategory { Name = "Vestidos", Category = clothing };
        var other = new Subcategory { Name = "Bolsos", Category = new Category { Name = "Accesorios" } };
        var size = new Size { Name = "M", SizeGroup = new SizeGroup { Name = "Ropa" } };

        foreach (var (subcategory, quantity) in new[] { (stocked, 2), (soldOut, 0), (other, 2) })
        {
            var product = new Product { IsPublishedInCatalog = subcategory != other, Name = subcategory.Name, SupplierProductCode = subcategory.Name, Subcategory = subcategory };
            context.ProductVariants.Add(new ProductVariant
            {
                Product = product, ProductPresentation = new ProductPresentation { Product = product },
                Size = size, AvailableQuantity = quantity, ReceivedQuantity = quantity
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new PublicCatalogService(context, new Resolver());
        var hiddenId = await context.Products.Where(product => !product.IsPublishedInCatalog)
            .Select(product => product.Id).SingleAsync();
        var page = await service.ListAsync(new CatalogQueryDTO());
        Assert.Equal("Blusas", Assert.Single(page.Items).Name);
        Assert.Null(await service.GetAsync(hiddenId));

        var filters = await service.FiltersAsync();

        var category = Assert.Single(filters.Categories);
        Assert.Equal("Ropa", category.Name);
        Assert.Equal("Blusas", Assert.Single(category.Subcategories).Name);
    }

    [Fact]
    public async Task FiltersAsync_ListsOnlyStockedPublishedSizesInDisplayOrder()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var clothing = new SizeGroup { Id = 2, Name = "Ropa" };
        var shoes = new SizeGroup { Id = 5, Name = "Calzado" };
        var hiddenGroup = new SizeGroup { Id = 6, Name = "Otros" };
        var published = new Product { IsPublishedInCatalog = true, Name = "Publicada", SupplierProductCode = "A" };
        var hidden = new Product { Name = "Oculta", SupplierProductCode = "B" };
        var publishedColor = new ProductPresentation { Product = published };
        var hiddenColor = new ProductPresentation { Product = hidden };
        var medium = new Size { Name = "M", SizeGroup = clothing, DisplayOrder = 2 };
        context.Sizes.Add(new Size { Name = "XL", SizeGroup = clothing, DisplayOrder = 4 });
        context.ProductVariants.AddRange(
            new ProductVariant { ProductPresentation = publishedColor, Product = published, Size = medium, AvailableQuantity = 1 },
            new ProductVariant { ProductPresentation = publishedColor, Product = published, Size =
                new Size { Name = "7", SizeGroup = shoes, DisplayOrder = 1 }, AvailableQuantity = 1 },
            new ProductVariant { ProductPresentation = publishedColor, Product = published, Size =
                new Size { Name = "S", SizeGroup = clothing, DisplayOrder = 1 }, AvailableQuantity = 1 },
            new ProductVariant { ProductPresentation = publishedColor, Product = published, Size =
                new Size { Name = "L", SizeGroup = clothing, DisplayOrder = 3 }, AvailableQuantity = 0 },
            new ProductVariant { ProductPresentation = hiddenColor, Product = hidden, Size =
                new Size { Name = "Única", SizeGroup = hiddenGroup, DisplayOrder = 1 }, AvailableQuantity = 1 });
        await context.SaveChangesAsync();

        var filters = await new PublicCatalogService(context, new Resolver()).FiltersAsync();

        Assert.Equal(new[] { "S", "M", "7" }, filters.Sizes.Select(size => size.Name));
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
        var small = new Size { Name = "S", SizeGroup = group, DisplayOrder = 2 };
        var medium = new Size { Name = "M", SizeGroup = group, DisplayOrder = 1 };
        var product = new Product { IsPublishedInCatalog = true, Name = "Blusa", SupplierProductCode = "A", Code = 1, Subcategory = subcategory };
        var blue = new ProductPresentation { Product = product, Name = "Azul", SortOrder = 1 };
        blue.Color = new Color { Name = "Azul", HexCode = "#2C5DAA" };
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

        Assert.Equal(new[] { "M", "S" }, all.Sizes);
        Assert.Equal("S", Assert.Single(filtered.Sizes));
        Assert.Equal(new[] { "Azul", "Rosa" }, all.Colors.Select(color => color.Name));
        Assert.Equal("Azul", Assert.Single(filtered.Colors).Name);
        Assert.Equal("#2C5DAA", Assert.Single(filtered.Colors).HexCode);
        Assert.Null(all.Colors.Single(option => option.Name == "Rosa").HexCode);

        var products = new ProductService(context, new Resolver());
        var catalogColor = Assert.Single(await products.GetColorsAsync(default));
        await products.UpdatePresentationColorAsync(product.Id, pink.Id, catalogColor.Id, default);
        var updated = Assert.Single((await service.ListAsync(new())).Items);
        Assert.Equal("#2C5DAA", updated.Colors.Single(option => option.Name == "Rosa").HexCode);
        Assert.Equal(catalogColor.Id, (await products.GetByIdAsync(product.Id)).Presentations.Single(option => option.Id == pink.Id).ColorId);
        await products.UpdatePresentationColorAsync(product.Id, pink.Id, null, default);
        var cleared = Assert.Single((await service.ListAsync(new())).Items);
        Assert.Null(cleared.Colors.Single(option => option.Name == "Rosa").HexCode);
        context.ChangeTracker.Clear();
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
