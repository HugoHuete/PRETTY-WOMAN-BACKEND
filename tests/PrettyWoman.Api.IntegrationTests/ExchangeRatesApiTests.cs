using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.Finances;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class ExchangeRatesApiTests(PrettyWomanApiFactory factory)
{
    private readonly PrettyWomanApiFactory _factory = factory;

    [Fact]
    public async Task Employee_CanGetCurrentExchangeRate()
    {
        var rateId = await SeedExchangeRateAsync();
        try
        {
            using var client = await CreateAuthenticatedClientAsync(
                PrettyWomanApiFactory.EmployeeEmail,
                PrettyWomanApiFactory.EmployeePassword,
                ensureEmployee: true);

            var response = await client.GetAsync("/api/v1/exchange-rates/current");
            var exchangeRate = await response.Content.ReadFromJsonAsync<CurrentExchangeRateDTO>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(exchangeRate);
            Assert.Equal(36.50m, exchangeRate.BankRate);
            Assert.Equal(37m, exchangeRate.StoreRate);
        }
        finally
        {
            await DeleteExchangeRateAsync(rateId);
        }
    }

    [Fact]
    public async Task Admin_CanGetCurrentExchangeRate()
    {
        var rateId = await SeedExchangeRateAsync();
        try
        {
            using var client = await CreateAuthenticatedClientAsync(
                PrettyWomanApiFactory.AdminUsername,
                PrettyWomanApiFactory.AdminPassword);

            var response = await client.GetAsync("/api/v1/exchange-rates/current");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await DeleteExchangeRateAsync(rateId);
        }
    }

    [Fact]
    public async Task GetCurrentExchangeRate_ReturnsNotFoundProblemDetailsWhenNoEnabledRateExists()
    {
        List<int> enabledRateIds;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var enabledRates = context.DollarExchangeRates.Where(rate => rate.Enabled).ToList();
            enabledRateIds = enabledRates.Select(rate => rate.Id).ToList();
            enabledRates.ForEach(rate => rate.Enabled = false);
            await context.SaveChangesAsync();
        }

        try
        {
            using var client = await CreateAuthenticatedClientAsync(
                PrettyWomanApiFactory.AdminUsername,
                PrettyWomanApiFactory.AdminPassword);

            var response = await client.GetAsync("/api/v1/exchange-rates/current");
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.NotNull(problem);
            Assert.Equal(404, problem.Status);
            Assert.Equal("No existe una tasa de cambio bancaria habilitada.", problem.Detail);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var rates = context.DollarExchangeRates
                .Where(rate => enabledRateIds.Contains(rate.Id))
                .ToList();
            rates.ForEach(rate => rate.Enabled = true);
            await context.SaveChangesAsync();
        }
    }

    private async Task<int> SeedExchangeRateAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rate = new DollarExchangeRate
        {
            BankRate = 36.50m,
            StoreRate = 37m,
            StartDate = DateTime.UtcNow.Date.AddDays(Random.Shared.Next(1, 10000)),
            Enabled = true
        };
        context.DollarExchangeRates.Add(rate);
        await context.SaveChangesAsync();
        return rate.Id;
    }

    private async Task DeleteExchangeRateAsync(int rateId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rate = await context.DollarExchangeRates.FindAsync(rateId);
        if (rate is not null)
        {
            context.DollarExchangeRates.Remove(rate);
            await context.SaveChangesAsync();
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password, bool ensureEmployee = false)
    {
        if (ensureEmployee)
        {
            await _factory.EnsureEmployeeAsync();
        }

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = username,
            Password = password
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private sealed class ProblemDetailsResponse
    {
        public int Status { get; set; }
        public string? Detail { get; set; }
    }
}
