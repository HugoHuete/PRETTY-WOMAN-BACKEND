using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.Products;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class ProductsApiTests(PrettyWomanApiFactory factory)
{
    private readonly PrettyWomanApiFactory _factory = factory;

    [Fact]
    public async Task EmployeeCanUpdateVariantPriceWithoutChangingProductHistory()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        using var client = await CreateEmployeeClientAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/products/{productVariant.ProductId}/variants/{productVariant.ProductVariantId}/price",
            new UpdateProductPriceDTO { SalePrice = 750m });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateVariantPrice_RejectsNonPositivePrice(decimal salePrice)
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        using var client = await CreateEmployeeClientAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/products/{productVariant.ProductId}/variants/{productVariant.ProductVariantId}/price",
            new UpdateProductPriceDTO { SalePrice = salePrice });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUserCannotUpdateVariantPrice()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        using var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/products/{productVariant.ProductId}/variants/{productVariant.ProductVariantId}/price",
            new UpdateProductPriceDTO { SalePrice = 750m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EmployeeCanExportProductsToExcel()
    {
        await _factory.SeedProductAsync(quantity: 2, receivedQuantity: 2, availableQuantity: 2, salePrice: 500m);
        using var client = await CreateEmployeeClientAsync();

        var response = await client.GetAsync("/api/v1/products/export");
        var content = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Content.Headers.ContentDisposition);
        Assert.EndsWith(".xlsx", response.Content.Headers.ContentDisposition.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName);
        Assert.Equal((byte)'P', content[0]);
        Assert.Equal((byte)'K', content[1]);
    }

    private async Task<HttpClient> CreateEmployeeClientAsync()
    {
        await _factory.EnsureEmployeeAsync();
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.EmployeeEmail,
            Password = PrettyWomanApiFactory.EmployeePassword
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
