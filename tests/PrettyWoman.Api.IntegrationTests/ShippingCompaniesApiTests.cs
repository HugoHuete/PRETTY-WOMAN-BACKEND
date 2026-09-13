using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.ShippingCompanies;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class ShippingCompaniesApiTests(PrettyWomanApiFactory factory)
{
    private readonly PrettyWomanApiFactory _factory = factory;

    [Fact]
    public async Task EmployeeCanListShippingCompanies()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.ShippingCompanies.AddRange(
                new Domain.Entities.ShippingCompany { Name = $"Zeta {Guid.NewGuid():N}" },
                new Domain.Entities.ShippingCompany { Name = $"Alfa {Guid.NewGuid():N}" });
            await context.SaveChangesAsync();
        }

        using var client = await CreateEmployeeClientAsync();
        var response = await client.GetAsync("/api/v1/shipping-companies");
        var result = await response.Content.ReadFromJsonAsync<List<ShippingCompanyDTO>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(result.OrderBy(item => item.Name).Select(item => item.Name), result.Select(item => item.Name));
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
