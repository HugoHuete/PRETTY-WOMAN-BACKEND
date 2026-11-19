using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.PublicCatalog;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class PublicCatalogTests(PrettyWomanApiFactory factory)
{
    [Fact]
    public async Task SelectionRateLimit_UsesClientIpOnlyFromTrustedProxy()
    {
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddTransient<IStartupFilter, TestPeerAddressFilter>()));
        using var client = app.CreateClient();
        for (var i = 0; i < 30; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync("100.64.1.1", "198.51.100.7")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync("100.64.1.1", "198.51.100.7")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync("100.64.1.1", "198.51.100.8")).StatusCode);
        for (var i = 0; i < 30; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync("203.0.113.10", $"198.51.100.{i + 10}")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync("203.0.113.10", "198.51.100.99")).StatusCode);

        async Task<HttpResponseMessage> SendAsync(string peer, string forwarded)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/catalog/selections");
            request.Headers.Add("X-Test-Peer", peer);
            request.Headers.Add("X-Real-IP", forwarded);
            request.Content = JsonContent.Create(new CreateSelectionDTO([]));
            return await client.SendAsync(request);
        }
    }

    private sealed class TestPeerAddressFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, continuation) =>
            {
                if (IPAddress.TryParse(context.Request.Headers["X-Test-Peer"].ToString(), out var peer))
                    context.Connection.RemoteIpAddress = peer;
                await continuation();
            });
            next(app);
        };
    }

    [Fact]
    public async Task AnonymousCatalogExcludesSoldOutProductsAndKeepsTheirDetails()
    {
        var available = await factory.SeedProductAsync(2, 2, 2);
        var soldOut = await factory.SeedProductAsync(1, 1, 0);
        using var client = factory.CreateClient();

        var page = await client.GetFromJsonAsync<CatalogPageDTO>("/api/v1/catalog/products?pageSize=60");
        Assert.NotNull(page);
        Assert.Contains(page.Items, product => product.Id == available.ProductId);
        Assert.DoesNotContain(page.Items, product => product.Id == soldOut.ProductId);

        var detail = await client.GetFromJsonAsync<CatalogDetailDTO>($"/api/v1/catalog/products/{soldOut.ProductId}");
        Assert.True(detail!.SoldOut);
        var json = await client.GetStringAsync($"/api/v1/catalog/products/{available.ProductId}");
        Assert.DoesNotContain("unitCost", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reservedQuantity", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/products")).StatusCode);
    }

    [Fact]
    public async Task PriceSortingAndSaleFiltering_UsePresentationDiscountAndPaginate()
    {
        var first = await factory.SeedProductAsync(2, 2, 2, 100m);
        var second = await factory.SeedProductAsync(2, 2, 2, 90m);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var firstProduct = await context.Products.FindAsync(first.ProductId);
        var secondProduct = await context.Products.FindAsync(second.ProductId);
        secondProduct!.SubcategoryId = firstProduct!.SubcategoryId;
        var campaign = new DiscountCampaign { Name = "Catalog price test", StartDate = DateTime.UtcNow.AddHours(-1), EndDate = DateTime.UtcNow.AddHours(1) };
        context.DiscountCampaignProducts.AddRange(
            new DiscountCampaignProduct { ProductId = first.ProductId, DiscountCampaign = campaign, DiscountTypeId = (int)DiscountTypeOption.FixedPrice, DiscountValue = 50m },
            new DiscountCampaignProduct { ProductPresentationId = first.ProductPresentationId, DiscountCampaign = campaign, DiscountTypeId = (int)DiscountTypeOption.FixedAmount, DiscountValue = 5m });
        await context.SaveChangesAsync();
        using var client = factory.CreateClient();
        var filter = $"subcategoryId={firstProduct.SubcategoryId}";

        var sorted = await client.GetFromJsonAsync<CatalogPageDTO>($"/api/v1/catalog/products?{filter}&sort=priceAsc&pageSize=1");
        var sale = await client.GetFromJsonAsync<CatalogPageDTO>($"/api/v1/catalog/products?{filter}&onSale=true");

        Assert.Equal(2, sorted!.TotalCount);
        Assert.Equal(second.ProductId, Assert.Single(sorted.Items).Id);
        var discounted = Assert.Single(sale!.Items);
        Assert.Equal(first.ProductId, discounted.Id);
        Assert.Equal(95m, discounted.FinalPrice);
        Assert.Equal(1, sale.TotalCount);
    }

    [Fact]
    public async Task ConcurrentSelectionRetriesRetainOriginalPriceAndReportCurrentStock()
    {
        var product = await factory.SeedProductAsync(2, 2, 2, 600);
        using var client = factory.CreateClient();
        var key = Guid.NewGuid();
        var attempts = await Task.WhenAll(CreateAsync(2, key), CreateAsync(2, key));
        var first = await attempts[0].Content.ReadFromJsonAsync<SelectionLinkDTO>();
        var second = await attempts[1].Content.ReadFromJsonAsync<SelectionLinkDTO>();
        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.InRange(first.ExpiresAt, DateTime.UtcNow.AddDays(30).AddMinutes(-1), DateTime.UtcNow.AddDays(30));
        Assert.Equal(HttpStatusCode.Conflict, (await CreateAsync(3, Guid.NewGuid())).StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var variant = await context.ProductVariants.FindAsync(product.ProductVariantId);
        variant!.AvailableQuantity = 1;
        variant.SalePrice = 750m;
        await context.SaveChangesAsync();

        var path = $"/api/v1/catalog/selections/{first.Id}";
        var selection = await client.GetFromJsonAsync<SelectionDetailDTO>(path);
        var line = Assert.Single(selection!.Items);
        Assert.Equal(600, line.Original.UnitPrice);
        Assert.Equal(2, line.Original.Quantity);
        Assert.Equal(1, line.AvailableQuantity);
        Assert.Equal(750, line.CurrentPrice);

        var row = await context.SharedCatalogSelections.FindAsync(first.Id);
        row!.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path)).StatusCode);

        async Task<HttpResponseMessage> CreateAsync(int quantity, Guid requestKey)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/catalog/selections");
            request.Headers.Add("Idempotency-Key", requestKey.ToString());
            request.Content = JsonContent.Create(new CreateSelectionDTO([new(product.ProductVariantId, quantity)]));
            return await client.SendAsync(request);
        }
    }
}
