using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.Orders;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class OrderReceiptApiTests(PrettyWomanApiFactory factory)
{
    [Fact]
    public async Task Employee_CannotUpdateOrderReceiptShippingCost()
    {
        await factory.EnsureEmployeeAsync();
        using var client = await CreateAuthenticatedClientAsync(
            PrettyWomanApiFactory.EmployeeEmail,
            PrettyWomanApiFactory.EmployeePassword);

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/orders/1/receipts/1")
        {
            Content = JsonContent.Create(new UpdateOrderReceiptDTO { WarehouseShippingCostUsd = 10m })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
