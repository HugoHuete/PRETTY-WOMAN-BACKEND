using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class OrdersApiTests(PrettyWomanApiFactory factory)
{
    private readonly PrettyWomanApiFactory _factory = factory;

    [Fact]
    public async Task CreateOrder_BindsNestedPresentationsAndReturnsGroupedSizes()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderDTO
        {
            SupplierId = catalog.SupplierId,
            PurchaseCurrencyId = 1,
            Products =
            [
                new CreateOrderProductDTO
                {
                    SupplierProductCode = $"API-{Guid.NewGuid():N}"[..12],
                    Name = "Producto contrato API",
                    SubcategoryId = catalog.SubcategoryId,
                    Presentations =
                    [
                        new CreateOrderProductPresentationDTO
                        {
                            Name = null,
                            SortOrder = 0,
                            Sizes =
                            [
                                new CreateOrderProductVariantDTO
                                {
                                    SizeId = catalog.SizeId,
                                    Quantity = 1,
                                    UnitCost = 10m,
                                    SalePrice = 39.99m
                                }
                            ]
                        },
                        new CreateOrderProductPresentationDTO
                        {
                            Name = "  Azul  ",
                            SortOrder = 1,
                            Sizes =
                            [
                                new CreateOrderProductVariantDTO
                                {
                                    SizeId = catalog.SizeId,
                                    Quantity = 3,
                                    UnitCost = 12.50m,
                                    SalePrice = 49.99m
                                }
                            ]
                        }
                    ]
                }
            ]
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var orderId = await response.Content.ReadFromJsonAsync<int>();
        var created = await client.GetFromJsonAsync<OrderDTO>($"/api/v1/orders/{orderId}");

        Assert.NotNull(created);
        var product = Assert.Single(created.Products);
        Assert.Equal(2, product.Presentations.Count);
        var unnamed = product.Presentations.ElementAt(0);
        var named = product.Presentations.ElementAt(1);
        var unnamedSize = Assert.Single(unnamed.Sizes);
        var namedSize = Assert.Single(named.Sizes);
        Assert.Null(unnamed.Name);
        Assert.Equal(0, unnamed.SortOrder);
        Assert.Equal("Azul", named.Name);
        Assert.Equal(1, named.SortOrder);
        Assert.Equal(catalog.SizeId, unnamedSize.SizeId);
        Assert.Equal(catalog.SizeId, namedSize.SizeId);
        Assert.Equal(1, unnamedSize.Quantity);
        Assert.Equal(3, namedSize.Quantity);
        Assert.Equal(39.99m, unnamedSize.SalePrice);
        Assert.Equal(49.99m, namedSize.SalePrice);
        Assert.All(product.Presentations, presentation => Assert.True(presentation.Id > 0));
        Assert.All(product.Presentations.SelectMany(presentation => presentation.Sizes), size => Assert.True(size.Id > 0));
    }

    [Fact]
    public async Task ListOrders_DoesNotReturnProductOrShortageDetails()
    {
        await _factory.SeedProductAsync(quantity: 2, receivedQuantity: 0, availableQuantity: 0);
        using var client = await CreateAdminClientAsync();

        var response = await client.GetAsync("/api/v1/orders?page=1&pageSize=20");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("\"products\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"purchaseShortages\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateOrder_ReusingProductReplacesPresentationsAndKeepsProductIdentity()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();
        var original = await client.GetFromJsonAsync<OrderDTO>($"/api/v1/orders/{seeded.OrderId}");

        Assert.NotNull(original);
        var originalProduct = Assert.Single(original.Products);
        var originalPresentation = Assert.Single(originalProduct.Presentations);
        Assert.Equal("Base", originalPresentation.Name);
        Assert.Single(originalPresentation.Sizes);

        var response = await client.PutAsJsonAsync($"/api/v1/orders/{seeded.OrderId}", new UpdateOrderDTO
        {
            SupplierId = catalog.SupplierId,
            PurchaseCurrencyId = 1,
            Products =
            [
                new CreateOrderProductDTO
                {
                    Id = catalog.ProductId,
                    SupplierProductCode = catalog.SupplierProductCode,
                    Name = catalog.ProductName,
                    SubcategoryId = catalog.SubcategoryId,
                    Presentations =
                    [
                        new CreateOrderProductPresentationDTO
                        {
                            Name = "  Verde  ",
                            SortOrder = 2,
                            Sizes =
                            [
                                new CreateOrderProductVariantDTO
                                {
                                    SizeId = catalog.SizeId,
                                    Quantity = 2,
                                    UnitCost = 15m,
                                    SalePrice = 65m
                                }
                            ]
                        }
                    ]
                }
            ]
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await client.GetFromJsonAsync<OrderDTO>($"/api/v1/orders/{seeded.OrderId}");

        Assert.NotNull(updated);
        var product = Assert.Single(updated.Products);
        var presentation = Assert.Single(product.Presentations);
        var size = Assert.Single(presentation.Sizes);
        Assert.Equal(catalog.ProductId, product.Id);
        Assert.Equal(catalog.ProductCode, product.Code);
        Assert.DoesNotContain(product.Presentations, item => item.Name == originalPresentation.Name);
        Assert.Equal("Verde", presentation.Name);
        Assert.Equal(2, presentation.SortOrder);
        Assert.Equal(catalog.SizeId, size.SizeId);
        Assert.Equal(2, size.Quantity);
        Assert.Equal(65m, size.SalePrice);
        Assert.NotEqual(originalPresentation.Id, presentation.Id);
        Assert.True(presentation.Id > 0);
        Assert.True(size.Id > 0);
    }

    [Fact]
    public async Task UpdateOrder_ReusingProductKeepsMatchingPresentationWithoutImages()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();
        var original = await client.GetFromJsonAsync<OrderDTO>($"/api/v1/orders/{seeded.OrderId}");

        Assert.NotNull(original);
        var originalPresentation = Assert.Single(original.Products.Single().Presentations);

        var response = await client.PutAsJsonAsync($"/api/v1/orders/{seeded.OrderId}", new UpdateOrderDTO
        {
            SupplierId = catalog.SupplierId,
            PurchaseCurrencyId = 1,
            Products =
            [
                new CreateOrderProductDTO
                {
                    Id = catalog.ProductId,
                    SupplierProductCode = catalog.SupplierProductCode,
                    Name = catalog.ProductName,
                    SubcategoryId = catalog.SubcategoryId,
                    Presentations =
                    [
                        new CreateOrderProductPresentationDTO
                        {
                            Name = "  Base  ",
                            SortOrder = 2,
                            Sizes = [CreateSize(catalog.SizeId)]
                        }
                    ]
                }
            ]
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await client.GetFromJsonAsync<OrderDTO>($"/api/v1/orders/{seeded.OrderId}");

        Assert.NotNull(updated);
        var presentation = Assert.Single(updated.Products.Single().Presentations);
        Assert.Equal(originalPresentation.Id, presentation.Id);
        Assert.Equal("Base", presentation.Name);
        Assert.Equal(2, presentation.SortOrder);
    }

    [Fact]
    public async Task UpdateOrder_ReusingProductKeepsPresentationImages()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var imageId = await _factory.SeedProductImageAsync(seeded.ProductId, seeded.ProductPresentationId, isPrimary: true, sortOrder: 0);
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();

        var response = await client.PutAsJsonAsync($"/api/v1/orders/{seeded.OrderId}", new UpdateOrderDTO
        {
            SupplierId = catalog.SupplierId,
            PurchaseCurrencyId = 1,
            Products =
            [
                new CreateOrderProductDTO
                {
                    Id = catalog.ProductId,
                    SupplierProductCode = catalog.SupplierProductCode,
                    Name = catalog.ProductName,
                    SubcategoryId = catalog.SubcategoryId,
                    Presentations =
                    [
                        new CreateOrderProductPresentationDTO
                        {
                            Name = "Base",
                            SortOrder = 0,
                            Sizes = [CreateSize(catalog.SizeId)]
                        }
                    ]
                }
            ]
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var image = await context.ProductImages.SingleOrDefaultAsync(item => item.Id == imageId);
        Assert.NotNull(image);
        Assert.Equal(seeded.ProductPresentationId, image.ProductPresentationId);
    }

    [Fact]
    public async Task UpdateOrder_RemovesOmittedPresentationAndItsImages()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var imageId = await _factory.SeedProductImageAsync(seeded.ProductId, seeded.ProductPresentationId, isPrimary: true, sortOrder: 0);
        using var beforeScope = _factory.Services.CreateScope();
        var beforeContext = beforeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediaAssetId = (await beforeContext.ProductImages
            .Where(item => item.Id == imageId)
            .Select(item => item.MediaAssetId)
            .SingleAsync()).GetValueOrDefault();
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();

        var response = await client.PutAsJsonAsync($"/api/v1/orders/{seeded.OrderId}", new UpdateOrderDTO
        {
            SupplierId = catalog.SupplierId,
            PurchaseCurrencyId = 1,
            Products =
            [
                new CreateOrderProductDTO
                {
                    Id = catalog.ProductId,
                    SupplierProductCode = catalog.SupplierProductCode,
                    Name = catalog.ProductName,
                    SubcategoryId = catalog.SubcategoryId,
                    Presentations =
                    [
                        new CreateOrderProductPresentationDTO
                        {
                            Name = "Verde",
                            SortOrder = 0,
                            Sizes = [CreateSize(catalog.SizeId)]
                        }
                    ]
                }
            ]
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Null(await context.ProductImages.SingleOrDefaultAsync(item => item.Id == imageId));
        Assert.Null(await context.ProductPresentations.SingleOrDefaultAsync(item => item.Id == seeded.ProductPresentationId));
        Assert.Null(await context.MediaAssets.SingleOrDefaultAsync(item => item.Id == mediaAssetId));

        var cleanupItems = await context.MediaCleanupItems
            .Where(item => item.MediaAssetId == mediaAssetId)
            .ToListAsync();
        Assert.Equal(2, cleanupItems.Count);
        Assert.All(cleanupItems, item => Assert.Equal(MediaCleanupStatus.Pending, item.Status));
        Assert.Contains(cleanupItems, item => item.StorageKey.EndsWith("/thumb.webp"));
        Assert.Contains(cleanupItems, item => item.StorageKey.EndsWith("/web.webp"));
    }

    [Fact]
    public async Task UpdateOrder_RemovesOmittedProductAndQueuesItsStorageObjects()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var imageId = await _factory.SeedProductImageAsync(seeded.ProductId, seeded.ProductPresentationId, isPrimary: true, sortOrder: 0);
        using var beforeScope = _factory.Services.CreateScope();
        var beforeContext = beforeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediaAssetId = (await beforeContext.ProductImages
            .Where(item => item.Id == imageId)
            .Select(item => item.MediaAssetId)
            .SingleAsync()).GetValueOrDefault();
        using var client = await CreateAdminClientAsync();

        var response = await client.PutAsJsonAsync($"/api/v1/orders/{seeded.OrderId}", new UpdateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = 1,
            Products = []
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Null(await context.Products.SingleOrDefaultAsync(item => item.Id == seeded.ProductId));
        Assert.Null(await context.MediaAssets.SingleOrDefaultAsync(item => item.Id == mediaAssetId));

        var cleanupItems = await context.MediaCleanupItems
            .Where(item => item.MediaAssetId == mediaAssetId)
            .ToListAsync();
        Assert.Equal(2, cleanupItems.Count);
        Assert.All(cleanupItems, item => Assert.Equal(MediaCleanupStatus.Pending, item.Status));
    }

    [Fact]
    public async Task CreateOrder_RejectsPresentationNamesThatDuplicateAfterNormalization()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/orders", BuildOrderRequest(catalog, [
            new CreateOrderProductPresentationDTO
            {
                Name = " Azul ",
                SortOrder = 0,
                Sizes = [CreateSize(catalog.SizeId)]
            },
            new CreateOrderProductPresentationDTO
            {
                Name = "azul",
                SortOrder = 1,
                Sizes = [CreateSize(catalog.SizeId)]
            }
        ]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_RejectsMoreThanOnePresentationWithoutName()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/orders", BuildOrderRequest(catalog, [
            new CreateOrderProductPresentationDTO
            {
                Name = null,
                SortOrder = 0,
                Sizes = [CreateSize(catalog.SizeId)]
            },
            new CreateOrderProductPresentationDTO
            {
                Name = " ",
                SortOrder = 1,
                Sizes = [CreateSize(catalog.SizeId)]
            }
        ]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrder_RejectsDuplicateProductIds()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();

        var product = new CreateOrderProductDTO
        {
            Id = catalog.ProductId,
            SupplierProductCode = catalog.SupplierProductCode,
            Name = catalog.ProductName,
            SubcategoryId = catalog.SubcategoryId,
            Presentations =
            [
                new CreateOrderProductPresentationDTO
                {
                    Name = "Base",
                    SortOrder = 0,
                    Sizes = [CreateSize(catalog.SizeId)]
                }
            ]
        };

        var response = await client.PutAsJsonAsync($"/api/v1/orders/{seeded.OrderId}", new UpdateOrderDTO
        {
            SupplierId = catalog.SupplierId,
            PurchaseCurrencyId = 1,
            Products = [product, product]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_RejectsNegativePresentationSortOrder()
    {
        var seeded = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 0, availableQuantity: 0);
        var catalog = await ReadSeededOrderDataAsync(seeded);
        using var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/orders", BuildOrderRequest(catalog, [
            new CreateOrderProductPresentationDTO
            {
                Name = "Azul",
                SortOrder = -1,
                Sizes = [CreateSize(catalog.SizeId)]
            }
        ]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static CreateOrderDTO BuildOrderRequest(
        SeededOrderData catalog,
        ICollection<CreateOrderProductPresentationDTO> presentations)
    {
        return new CreateOrderDTO
        {
            SupplierId = catalog.SupplierId,
            PurchaseCurrencyId = 1,
            Products =
            [
                new CreateOrderProductDTO
                {
                    SupplierProductCode = $"API-{Guid.NewGuid():N}"[..12],
                    Name = "Producto validación API",
                    SubcategoryId = catalog.SubcategoryId,
                    Presentations = presentations
                }
            ]
        };
    }

    private static CreateOrderProductVariantDTO CreateSize(int sizeId)
        => new() { SizeId = sizeId, Quantity = 1, UnitCost = 10m, SalePrice = 40m };

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.AdminUsername,
            Password = PrettyWomanApiFactory.AdminPassword
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private async Task<SeededOrderData> ReadSeededOrderDataAsync(PrettyWomanApiFactory.SeededProduct seeded)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = await context.Orders
            .Include(item => item.ProductVariants)
                .ThenInclude(item => item.Product)
            .SingleAsync(item => item.Id == seeded.OrderId);
        var variant = order.ProductVariants.Single(item => item.Id == seeded.ProductVariantId);

        return new(
            order.SupplierId,
            variant.Product!.Id,
            variant.Product.Code,
            variant.Product.SupplierProductCode,
            variant.Product.Name,
            variant.Product.SubcategoryId,
            variant.SizeId);
    }

    private sealed record SeededOrderData(
        int SupplierId,
        int ProductId,
        int ProductCode,
        string SupplierProductCode,
        string ProductName,
        int SubcategoryId,
        int SizeId);
}
