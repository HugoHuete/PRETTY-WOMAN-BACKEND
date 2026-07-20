using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class DashboardSummaryTests(PrettyWomanApiFactory factory)
{
    private readonly PrettyWomanApiFactory _factory = factory;

    [Fact]
    public async Task Admin_CanGetSummaryForRequestedPeriod_WithFinancialBlock()
    {
        using var client = await CreateAuthenticatedClientAsync(
            PrettyWomanApiFactory.AdminEmail,
            PrettyWomanApiFactory.AdminPassword);

        var response = await client.GetAsync("/api/v1/dashboard/summary?fromDate=2099-01-01&toDate=2099-01-01");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2099-01-01", body.RootElement.GetProperty("fromDate").GetString());
        Assert.Equal("2099-01-01", body.RootElement.GetProperty("toDate").GetString());
        Assert.True(body.RootElement.TryGetProperty("financial", out _));
        Assert.Equal(0, body.RootElement.GetProperty("sales").GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Employee_CanGetOperationalSummary_WithoutFinancialBlock()
    {
        await _factory.EnsureEmployeeAsync();
        using var client = await CreateAuthenticatedClientAsync(
            PrettyWomanApiFactory.EmployeeEmail,
            PrettyWomanApiFactory.EmployeePassword);

        var response = await client.GetAsync("/api/v1/dashboard/summary?fromDate=2099-01-01&toDate=2099-01-01");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.TryGetProperty("operations", out var operations));
        Assert.Equal(0, operations.GetProperty("activeReservationCount").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("financial", out _));
    }

    [Fact]
    public async Task Summary_AggregatesTheRequestedPeriod()
    {
        var date = new DateTime(2098, 12, 30, 12, 0, 0, DateTimeKind.Utc);
        await SeedDashboardDataAsync(date);
        using var client = await CreateAuthenticatedClientAsync(
            PrettyWomanApiFactory.AdminEmail,
            PrettyWomanApiFactory.AdminPassword);

        var response = await client.GetAsync("/api/v1/dashboard/summary?fromDate=2098-12-30&toDate=2098-12-30");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.RootElement.GetProperty("sales").GetProperty("count").GetInt32());
        Assert.Equal(1000m, body.RootElement.GetProperty("sales").GetProperty("totalNio").GetDecimal());
        Assert.Equal(600m, body.RootElement.GetProperty("sales").GetProperty("pendingCollectionNio").GetDecimal());
        Assert.Equal(400.15m, body.RootElement.GetProperty("payments").GetProperty("collectedNio").GetDecimal());
        Assert.Equal(1, body.RootElement.GetProperty("operations").GetProperty("activeReservationCount").GetInt32());
        Assert.Equal(2, body.RootElement.GetProperty("operations").GetProperty("activeReservedUnitCount").GetInt32());
        Assert.Equal(1, body.RootElement.GetProperty("operations").GetProperty("openInventoryIssueCount").GetInt32());
        Assert.Equal(1, body.RootElement.GetProperty("operations").GetProperty("openInventoryIssueUnitCount").GetInt32());
        Assert.Equal(500m, body.RootElement.GetProperty("financial").GetProperty("incomeNio").GetDecimal());
        Assert.Equal(200m, body.RootElement.GetProperty("financial").GetProperty("expenseNio").GetDecimal());
        Assert.Equal(300m, body.RootElement.GetProperty("financial").GetProperty("balanceNio").GetDecimal());
    }

    [Fact]
    public async Task Summary_WithInvalidDateRange_ReturnsBadRequest()
    {
        using var client = await CreateAuthenticatedClientAsync(
            PrettyWomanApiFactory.AdminEmail,
            PrettyWomanApiFactory.AdminPassword);

        var response = await client.GetAsync("/api/v1/dashboard/summary?fromDate=2099-01-02&toDate=2099-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Summary_WithMaximumEndDate_ReturnsBadRequest()
    {
        using var client = await CreateAuthenticatedClientAsync(
            PrettyWomanApiFactory.AdminEmail,
            PrettyWomanApiFactory.AdminPassword);

        var response = await client.GetAsync("/api/v1/dashboard/summary?fromDate=9999-12-31&toDate=9999-12-31");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = email,
            Password = password
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private async Task SeedDashboardDataAsync(DateTime date)
    {
        var product = await _factory.SeedProductAsync(quantity: 2, receivedQuantity: 2, availableQuantity: 2);
        var location = await _factory.SeedDeliveryLocationAsync();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var admin = await userManager.FindByEmailAsync(PrettyWomanApiFactory.AdminEmail)
            ?? throw new InvalidOperationException("No se encontró el administrador de integración.");

        var sale = new Sale
        {
            SaleDate = date,
            SaleChannelId = (int)SaleChannelOption.InStoreSale,
            SaleStatusId = (int)SaleStatusOption.Completed,
            SalePaymentStatusId = (int)SalePaymentStatusOption.PartiallyPaid,
            UserId = admin.Id,
            Subtotal = 1000m,
            Total = 1000m
        };
        context.Sales.Add(sale);
        context.AddRange(
            new SalePaymentMovement
            {
                Sale = sale,
                MovementDate = date,
                MovementDirectionId = (int)MovementDirectionOptions.In,
                PaymentMethodId = (int)PaymentMethodOption.Cash,
                GrossAmount = 400m,
                ProductAmount = 400m,
                NetReceivedAmount = 400m,
                AmountReceivedUsd = 10.96m,
                ExchangeRate = 36.51m,
                ExchangeDifferenceNio = 0.15m,
                UserId = admin.Id
            },
            new ProductHold
            {
                ProductId = product.ProductId,
                Quantity = 2,
                HoldDate = date,
                HoldReason = "Reserva de integración",
                ProductHoldStatusId = (int)ProductHoldStatusOption.Active
            },
            new SaleDelivery
            {
                Sale = sale,
                Code = $"DASH-{Guid.NewGuid():N}",
                MunicipalityId = location.MunicipalityId,
                DeliveryAgencyId = location.DeliveryAgencyId,
                DeliveryStatusId = (int)DeliveryStatusCode.Pending,
                UserId = admin.Id,
                CreatedAt = date
            },
            new ProductInventoryIssue
            {
                ProductId = product.ProductId,
                ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Damaged,
                ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.Open,
                Quantity = 1,
                IssueDate = date
            },
            new FinancialMovement
            {
                Description = "Ingreso dashboard integración",
                MovementDate = date,
                MovementDirectionId = (int)MovementDirectionOptions.In,
                FinancialMovementTypeId = (int)FinancialMovementTypeOption.OwnerInvestment,
                Amount = 500m,
                ExchangeRate = 36m
            },
            new FinancialMovement
            {
                Description = "Egreso dashboard integración",
                MovementDate = date,
                MovementDirectionId = (int)MovementDirectionOptions.Out,
                FinancialMovementTypeId = (int)FinancialMovementTypeOption.Expense,
                Amount = 200m,
                ExchangeRate = 36m
            });
        await context.SaveChangesAsync();
    }
}
