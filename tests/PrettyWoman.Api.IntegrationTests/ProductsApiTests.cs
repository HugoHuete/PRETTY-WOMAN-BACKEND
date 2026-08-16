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

    private async Task<HttpClient> CreateEmployeeClientAsync()
    {
        await _factory.EnsureEmployeeAsync();
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = PrettyWomanApiFactory.EmployeeEmail,
            Password = PrettyWomanApiFactory.EmployeePassword
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
