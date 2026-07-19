using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.InventoryAdjustments;
using PrettyWoman.Application.DTOs.InventoryCatalogs;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class InventoryAdjustmentApiTests(PrettyWomanApiFactory factory)
{
    private readonly PrettyWomanApiFactory _factory = factory;

    [Fact]
    public async Task Employee_CanReadInventoryCatalogs()
    {
        using var employee = await CreateEmployeeClientAsync();

        var reasonsResponse = await employee.GetAsync("/api/v1/inventory-catalogs/adjustment-reasons");
        var suggestionsResponse = await employee.GetAsync("/api/v1/inventory-catalogs/adjustment-reason-suggestions");
        var bucketsResponse = await employee.GetAsync("/api/v1/inventory-catalogs/stock-buckets");

        Assert.Equal(HttpStatusCode.OK, reasonsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, suggestionsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bucketsResponse.StatusCode);

        var reasons = await reasonsResponse.Content.ReadFromJsonAsync<List<InventoryCatalogItemDTO>>();
        var suggestions = await suggestionsResponse.Content.ReadFromJsonAsync<List<InventoryAdjustmentReasonSuggestionDTO>>();
        var buckets = await bucketsResponse.Content.ReadFromJsonAsync<List<InventoryCatalogItemDTO>>();

        Assert.NotNull(reasons);
        Assert.NotNull(suggestions);
        Assert.NotNull(buckets);
        Assert.Contains(reasons, reason =>
            reason.Id == (int)InventoryAdjustmentReasonOption.ManualCorrection &&
            reason.Name == nameof(InventoryAdjustmentReasonOption.ManualCorrection));
        Assert.Contains(suggestions, suggestion =>
            suggestion.InventoryAdjustmentReasonId == (int)InventoryAdjustmentReasonOption.PurchaseSurplus &&
            suggestion.SuggestedMovements.Count == 0 &&
            !string.IsNullOrWhiteSpace(suggestion.Description));
        Assert.Contains(buckets, bucket =>
            bucket.Id == (int)InventoryStockBucketOption.Available &&
            bucket.Name == nameof(InventoryStockBucketOption.Available));
        Assert.Equal(reasons.OrderBy(reason => reason.Id).Select(reason => reason.Id), reasons.Select(reason => reason.Id));
        Assert.Equal(buckets.OrderBy(bucket => bucket.Id).Select(bucket => bucket.Id), buckets.Select(bucket => bucket.Id));
    }

    [Fact]
    public async Task Employee_CanReadButCannotCreateInventoryAdjustments()
    {
        using var employee = await CreateEmployeeClientAsync();

        var readResponse = await employee.GetAsync("/api/v1/inventory-adjustments");
        var createResponse = await employee.PostAsJsonAsync("/api/v1/inventory-adjustments", new CreateInventoryAdjustmentDTO
        {
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.ManualCorrection,
            Items =
            [
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = 1,
                    FromStockBucketId = (int)InventoryStockBucketOption.Available,
                    ToStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                    Quantity = 1
                }
            ]
        });

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateAndQueryInventoryAdjustment()
    {
        var product = await _factory.SeedProductAsync(quantity: 4, receivedQuantity: 4, availableQuantity: 4);
        using var admin = await CreateAdminClientAsync();
        var adjustmentDate = DateTime.UtcNow.AddMinutes(-10);

        var createResponse = await admin.PostAsJsonAsync("/api/v1/inventory-adjustments", new CreateInventoryAdjustmentDTO
        {
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.LostItem,
            AdjustmentDate = adjustmentDate,
            Reference = "AJ-INTEGRATION-001",
            Comments = "Ajuste creado desde prueba de integración.",
            Items =
            [
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = product.ProductId,
                    FromStockBucketId = (int)InventoryStockBucketOption.Available,
                    ToStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                    Quantity = 2,
                    Comments = "Dos unidades dadas de baja."
                }
            ]
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var adjustmentId = await createResponse.Content.ReadFromJsonAsync<int>();
        Assert.True(adjustmentId > 0);

        var stock = await _factory.GetProductStockAsync(product.ProductId);
        Assert.Equal(4, stock.ReceivedQuantity);
        Assert.Equal(2, stock.AvailableQuantity);
        Assert.Equal(0, stock.ReservedQuantity);
        Assert.Equal(0, stock.UnavailableQuantity);

        var detailResponse = await admin.GetAsync($"/api/v1/inventory-adjustments/{adjustmentId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<InventoryAdjustmentDTO>();

        Assert.NotNull(detail);
        Assert.Equal(adjustmentId, detail.Id);
        Assert.Equal((int)InventoryAdjustmentReasonOption.LostItem, detail.InventoryAdjustmentReasonId);
        Assert.Equal(nameof(InventoryAdjustmentReasonOption.LostItem), detail.InventoryAdjustmentReasonName);
        Assert.Equal("AJ-INTEGRATION-001", detail.Reference);
        Assert.Equal("Ajuste creado desde prueba de integración.", detail.Comments);
        var item = Assert.Single(detail.Items);
        Assert.Equal(product.ProductId, item.ProductId);
        Assert.Equal((int)InventoryStockBucketOption.Available, item.FromStockBucketId);
        Assert.Equal(nameof(InventoryStockBucketOption.Available), item.FromStockBucketName);
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, item.ToStockBucketId);
        Assert.Equal(nameof(InventoryStockBucketOption.OutOfInventory), item.ToStockBucketName);
        Assert.Equal(2, item.Quantity);
        Assert.True(item.InventoryMovementId > 0);

        var listResponse = await admin.GetAsync($"/api/v1/inventory-adjustments?productId={product.ProductId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedResult<InventoryAdjustmentDTO>>();

        Assert.NotNull(list);
        Assert.True(list.TotalCount >= 1);
        Assert.Contains(list.Items, adjustment => adjustment.Id == adjustmentId);
    }

    [Fact]
    public async Task Admin_CannotCreateAdjustmentWhenSourceBucketHasInsufficientStock()
    {
        var product = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1);
        using var admin = await CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/inventory-adjustments", new CreateInventoryAdjustmentDTO
        {
            InventoryAdjustmentReasonId = (int)InventoryAdjustmentReasonOption.LostItem,
            Items =
            [
                new CreateInventoryAdjustmentItemDTO
                {
                    ProductId = product.ProductId,
                    FromStockBucketId = (int)InventoryStockBucketOption.Available,
                    ToStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                    Quantity = 2
                }
            ]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var stock = await _factory.GetProductStockAsync(product.ProductId);
        Assert.Equal(1, stock.ReceivedQuantity);
        Assert.Equal(1, stock.AvailableQuantity);
        Assert.Equal(0, stock.ReservedQuantity);
        Assert.Equal(0, stock.UnavailableQuantity);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
        => await CreateAuthenticatedClientAsync(PrettyWomanApiFactory.AdminEmail, PrettyWomanApiFactory.AdminPassword);

    private async Task<HttpClient> CreateEmployeeClientAsync()
    {
        await _factory.EnsureEmployeeAsync();
        return await CreateAuthenticatedClientAsync(PrettyWomanApiFactory.EmployeeEmail, PrettyWomanApiFactory.EmployeePassword);
    }

    private HttpClient CreateAnonymousClient()
        => _factory.CreateClient();

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = CreateAnonymousClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
