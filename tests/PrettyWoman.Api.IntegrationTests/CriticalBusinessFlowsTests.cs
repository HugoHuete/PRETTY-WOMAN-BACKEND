using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class CriticalBusinessFlowsTests(PrettyWomanApiFactory factory)
{
    private readonly PrettyWomanApiFactory _factory = factory;

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task DeliveryCancellation_AndDeliveryMutation_SerializeOnSale(bool cancellationFirst, bool send)
    {
        var product = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        var location = await _factory.SeedDeliveryLocationAsync();
        using var admin = await CreateAdminClientAsync();
        var saleResponse = await admin.PostAsJsonAsync("/api/v1/sales", new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            ProductVariants = [new CreateSaleProductDTO { ProductId = product.ProductVariantId, Quantity = 1 }]
        });
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
        var saleId = await saleResponse.Content.ReadFromJsonAsync<int>();
        var deliveryResponse = await admin.PostAsJsonAsync($"/api/v1/sales/{saleId}/deliveries", new CreateSaleDeliveryDTO
        {
            Code = $"RACE-{Guid.NewGuid():N}"[..20],
            MunicipalityId = location.MunicipalityId,
            DeliveryAgencyId = location.DeliveryAgencyId,
            ShippingChargedToClient = 50m
        });
        Assert.Equal(HttpStatusCode.Created, deliveryResponse.StatusCode);
        var deliveryId = await deliveryResponse.Content.ReadFromJsonAsync<int>();

        await using var ownerScope = _factory.Services.CreateAsyncScope();
        var ownerContext = ownerScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ownerService = ownerScope.ServiceProvider.GetRequiredService<ISaleService>();
        await using var transaction = await ownerContext.BeginSalePaymentTransactionAsync(saleId);
        Assert.NotNull(transaction);
        await using var waitingScope = _factory.Services.CreateAsyncScope();
        var waitingService = waitingScope.ServiceProvider.GetRequiredService<ISaleService>();

        Task MutateAsync(ISaleService service) => send
            ? service.MarkDeliveryAsSentAsync(saleId, deliveryId)
            : service.UpdateDeliveryAsync(saleId, deliveryId, new PatchSaleDeliveryDTO { ShippingChargedToClient = 75m });

        var waitingOperation = cancellationFirst
            ? MutateAsync(waitingService)
            : waitingService.CancelDeliveryAsync(saleId, deliveryId);
        await WaitForSaleLockAsync();
        if (cancellationFirst)
            await ownerService.CancelDeliveryAsync(saleId, deliveryId);
        else
            await MutateAsync(ownerService);
        await transaction.CommitAsync();

        if (cancellationFirst || send)
            await Assert.ThrowsAsync<AppBadRequestException>(() => waitingOperation);
        else
            await waitingOperation;

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        Assert.Equal((int)(!cancellationFirst && send ? DeliveryStatusCode.Sent : DeliveryStatusCode.Cancelled), delivery.DeliveryStatusId);
        Assert.Equal(!cancellationFirst && send ? 50m : 0m, delivery.ShippingChargedToClient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeliveryCancellation_AndShippingPayment_SerializeOnSale(bool cancellationFirst)
    {
        var product = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        var location = await _factory.SeedDeliveryLocationAsync();
        using var admin = await CreateAdminClientAsync();
        var saleResponse = await admin.PostAsJsonAsync("/api/v1/sales", new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            ProductVariants = [new CreateSaleProductDTO { ProductId = product.ProductVariantId, Quantity = 1 }]
        });
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
        var saleId = await saleResponse.Content.ReadFromJsonAsync<int>();
        var deliveryResponse = await admin.PostAsJsonAsync($"/api/v1/sales/{saleId}/deliveries", new CreateSaleDeliveryDTO
        {
            Code = $"LOCK-{Guid.NewGuid():N}"[..20],
            MunicipalityId = location.MunicipalityId,
            DeliveryAgencyId = location.DeliveryAgencyId,
            ShippingChargedToClient = 50m
        });
        Assert.Equal(HttpStatusCode.Created, deliveryResponse.StatusCode);
        var deliveryId = await deliveryResponse.Content.ReadFromJsonAsync<int>();
        var payment = new CreateSalePaymentMovementDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Transfer,
            ShippingAmount = 50m,
            SaleDeliveryId = deliveryId
        };

        await using var ownerScope = _factory.Services.CreateAsyncScope();
        var ownerContext = ownerScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ownerService = ownerScope.ServiceProvider.GetRequiredService<ISaleService>();
        await using var transaction = await ownerContext.BeginSalePaymentTransactionAsync(saleId);
        Assert.NotNull(transaction);
        await using var waitingScope = _factory.Services.CreateAsyncScope();
        var waitingService = waitingScope.ServiceProvider.GetRequiredService<ISaleService>();
        Task waitingOperation = cancellationFirst
            ? waitingService.AddPaymentMovementAsync(saleId, payment)
            : waitingService.CancelDeliveryAsync(saleId, deliveryId);

        await WaitForSaleLockAsync();
        if (cancellationFirst)
            await ownerService.CancelDeliveryAsync(saleId, deliveryId);
        else
            await ownerService.AddPaymentMovementAsync(saleId, payment);
        await transaction.CommitAsync();

        if (cancellationFirst)
            await Assert.ThrowsAsync<AppBadRequestException>(() => waitingOperation);
        else
            await waitingOperation;

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var delivery = await verificationContext.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        var movements = await verificationContext.SalePaymentMovements.Where(item => item.SaleDeliveryId == deliveryId).ToListAsync();
        Assert.Equal((int)DeliveryStatusCode.Cancelled, delivery.DeliveryStatusId);
        Assert.Equal(0m, movements.Sum(item => item.MovementDirectionId == (int)MovementDirectionOptions.In ? item.ShippingAmount : -item.ShippingAmount));
        Assert.Equal(cancellationFirst ? 0 : 2, movements.Count);
    }

    private async Task WaitForSaleLockAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var waiting = await context.Database.SqlQueryRaw<int>(
                "SELECT count(*)::int AS \"Value\" FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%FOR UPDATE%' AND pid <> pg_backend_pid()")
                .SingleAsync();
            if (waiting > 0) return;
            await Task.Delay(50);
        }

        throw new TimeoutException("La operación concurrente no esperó el bloqueo de la venta.");
    }

    [Fact]
    public async Task InStoreSale_WithPartialThenFullPayment_CompletesAndReducesInventory()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 2, receivedQuantity: 2, availableQuantity: 2);
        using var client = await CreateEmployeeClientAsync();

        var saleId = await CreateSaleAsync(client, productVariant.ProductVariantId, initialPayment: 400m);
        var partiallyPaidSale = await GetSaleAsync(client, saleId);

        Assert.Equal((int)SalePaymentStatusOption.PartiallyPaid, partiallyPaidSale.SalePaymentStatusId);
        Assert.Equal((int)SaleStatusOption.Pending, partiallyPaidSale.SaleStatusId);
        Assert.Equal(2, (await _factory.GetProductStockAsync(productVariant.ProductVariantId)).AvailableQuantity);

        var finalPayment = await client.PostAsJsonAsync($"/api/v1/sales/{saleId}/payment-movements", new CreateSalePaymentMovementDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Cash,
            ProductAmount = 600m
        });

        Assert.Equal(HttpStatusCode.Created, finalPayment.StatusCode);

        var completedSale = await GetSaleAsync(client, saleId);
        Assert.Equal((int)SalePaymentStatusOption.Paid, completedSale.SalePaymentStatusId);
        Assert.Equal((int)SaleStatusOption.Completed, completedSale.SaleStatusId);
        Assert.Equal(1, (await _factory.GetProductStockAsync(productVariant.ProductVariantId)).AvailableQuantity);
        Assert.Equal(2, completedSale.PaymentMovements.Count);
    }

    [Fact]
    public async Task AdminRefund_ReopensCompletedSaleAndPreservesInventoryReservation()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        using var admin = await CreateAdminClientAsync();

        var saleId = await CreateSaleAsync(admin, productVariant.ProductVariantId, initialPayment: 500m);
        var paymentId = (await GetSaleAsync(admin, saleId)).PaymentMovements.Single().Id;

        var refund = await admin.PostAsJsonAsync($"/api/v1/sales/{saleId}/payment-movements/{paymentId}/refunds", new RefundSalePaymentMovementDTO());

        Assert.Equal(HttpStatusCode.Created, refund.StatusCode);

        var reopenedSale = await GetSaleAsync(admin, saleId);
        Assert.Equal((int)SalePaymentStatusOption.Unpaid, reopenedSale.SalePaymentStatusId);
        Assert.Equal((int)SaleStatusOption.Reserved, reopenedSale.SaleStatusId);
        Assert.Equal(2, reopenedSale.PaymentMovements.Count);
        Assert.Equal(0, (await _factory.GetProductStockAsync(productVariant.ProductVariantId)).AvailableQuantity);
    }

    [Fact]
    public async Task AdminCanReceivePurchaseInParts_AndInventoryTracksEachReceipt()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 2, receivedQuantity: 0, availableQuantity: 0);
        using var admin = await CreateAdminClientAsync();

        var firstReceipt = await ReceiveAsync(admin, productVariant.OrderId, productVariant.ProductVariantId, 1);
        Assert.Equal(HttpStatusCode.OK, firstReceipt.StatusCode);
        Assert.Equal(1, (await _factory.GetProductStockAsync(productVariant.ProductVariantId)).ReceivedQuantity);
        Assert.Equal(1, (await _factory.GetProductStockAsync(productVariant.ProductVariantId)).AvailableQuantity);

        var secondReceipt = await ReceiveAsync(admin, productVariant.OrderId, productVariant.ProductVariantId, 1);
        Assert.Equal(HttpStatusCode.OK, secondReceipt.StatusCode);

        var stock = await _factory.GetProductStockAsync(productVariant.ProductVariantId);
        Assert.Equal(2, stock.ReceivedQuantity);
        Assert.Equal(2, stock.AvailableQuantity);
    }

    [Fact]
    public async Task AdminCanReceiveExplicitPurchaseSurplus()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 2, receivedQuantity: 0, availableQuantity: 0);
        using var admin = await CreateAdminClientAsync();

        var receipt = await admin.PostAsJsonAsync($"/api/v1/orders/{productVariant.OrderId}/receipts", new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 0,
            ProductVariants =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = productVariant.ProductVariantId,
                    Quantity = 3,
                    Weight = 1,
                    IsSurplus = true
                }
            ]
        });

        Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);

        var stock = await _factory.GetProductStockAsync(productVariant.ProductVariantId);
        Assert.Equal(3, stock.ReceivedQuantity);
        Assert.Equal(3, stock.AvailableQuantity);
    }

    [Fact]
    public async Task AdminCanReceiveInStoreReturn_AndRestoresAvailableInventory()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        using var admin = await CreateAdminClientAsync();
        var saleId = await CreateSaleAsync(admin, productVariant.ProductVariantId, initialPayment: 500m);
        var originalSaleProductId = (await GetSaleAsync(admin, saleId)).ProductVariants.Single().Id;

        var createReturn = await admin.PostAsJsonAsync($"/api/v1/sales/{saleId}/returns", new CreateSaleReturnDTO
        {
            ReasonId = (int)SaleReturnReasonOption.CustomerPreference,
            MethodId = (int)SaleReturnMethodOption.InStore,
            Items = [new CreateSaleReturnItemDTO { OriginalSaleProductId = originalSaleProductId, Quantity = 1, RecognizedUnitAmount = 500m }]
        });
        Assert.Equal(HttpStatusCode.Created, createReturn.StatusCode);
        var returnId = await createReturn.Content.ReadFromJsonAsync<int>();
        var returnItemId = (await GetReturnsAsync(admin, saleId)).Single(item => item.Id == returnId).Items.Single().Id;

        var receive = await admin.PostAsJsonAsync($"/api/v1/sales/{saleId}/returns/{returnId}/receive", new ReceiveSaleReturnDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Cash,
            Items = [new ReceiveSaleReturnItemDTO { SaleReturnItemId = returnItemId, IsDamaged = false }]
        });

        Assert.Equal(HttpStatusCode.NoContent, receive.StatusCode);
        Assert.Equal(1, (await _factory.GetProductStockAsync(productVariant.ProductVariantId)).AvailableQuantity);
    }

    [Fact]
    public async Task AdminCanCompleteExchange_AndMovesBothProductsThroughInventory()
    {
        var original = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        var replacement = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 700m);
        using var admin = await CreateAdminClientAsync();
        var saleId = await CreateSaleAsync(admin, original.ProductVariantId, initialPayment: 500m);
        var originalSaleProductId = (await GetSaleAsync(admin, saleId)).ProductVariants.Single().Id;

        var createExchange = await admin.PostAsJsonAsync($"/api/v1/sales/{saleId}/exchanges", new CreateSaleExchangeDTO
        {
            ReturnItems = [new CreateExchangeReturnItemDTO { OriginalSaleProductId = originalSaleProductId, Quantity = 1, RecognizedUnitAmount = 500m }],
            OutboundItems = [new CreateExchangeOutboundItemDTO { ProductId = replacement.ProductVariantId, Quantity = 1, ItemTypeId = (int)ExchangeOutboundItemTypeOption.Replacement }]
        });
        Assert.Equal(HttpStatusCode.Created, createExchange.StatusCode);
        var exchangeId = await createExchange.Content.ReadFromJsonAsync<int>();

        var handover = await admin.PostAsync($"/api/v1/sales/{saleId}/exchanges/{exchangeId}/handover", content: null);
        Assert.Equal(HttpStatusCode.NoContent, handover.StatusCode);
        var returnItemId = (await GetExchangesAsync(admin, saleId)).Single(item => item.Id == exchangeId).ReturnItems.Single().Id;
        var receive = await admin.PostAsync($"/api/v1/sales/{saleId}/exchanges/{exchangeId}/return-items/{returnItemId}/received", content: null);

        Assert.Equal(HttpStatusCode.NoContent, receive.StatusCode);
        Assert.Equal(1, (await _factory.GetProductStockAsync(original.ProductVariantId)).AvailableQuantity);
        Assert.Equal(0, (await _factory.GetProductStockAsync(replacement.ProductVariantId)).AvailableQuantity);
    }

    [Fact]
    public async Task AdminCanReconcileSentCashOnDelivery_AndCompletesTheSaleWhenFullyCollected()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        var location = await _factory.SeedDeliveryLocationAsync();
        using var admin = await CreateAdminClientAsync();

        var sale = await admin.PostAsJsonAsync("/api/v1/sales", new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            ProductVariants = [new CreateSaleProductDTO { ProductId = productVariant.ProductVariantId, Quantity = 1 }]
        });
        Assert.Equal(HttpStatusCode.Created, sale.StatusCode);
        var saleId = await sale.Content.ReadFromJsonAsync<int>();
        var delivery = await admin.PostAsJsonAsync($"/api/v1/sales/{saleId}/deliveries", new CreateSaleDeliveryDTO
        {
            Code = $"COD-{Guid.NewGuid():N}"[..20],
            MunicipalityId = location.MunicipalityId,
            DeliveryAgencyId = location.DeliveryAgencyId,
            ShippingChargedToClient = 50m
        });
        Assert.Equal(HttpStatusCode.Created, delivery.StatusCode);
        var deliveryId = await delivery.Content.ReadFromJsonAsync<int>();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/v1/sales/{saleId}/deliveries/{deliveryId}/send", null)).StatusCode);

        var reconciliation = await admin.PostAsJsonAsync("/api/v1/deliveryagencyreconciliations", new CreateDeliveryAgencyReconciliationDTO
        {
            DeliveryAgencyId = location.DeliveryAgencyId,
            SettlementExchangeRate = 36m,
            Deliveries = [new ReconcileSaleDeliveryDTO { SaleDeliveryId = deliveryId, AmountCollectedNio = 550m }]
        });

        Assert.Equal(HttpStatusCode.OK, reconciliation.StatusCode);
        Assert.True(await reconciliation.Content.ReadFromJsonAsync<int>() > 0);
        var completedSale = await GetSaleAsync(admin, saleId);
        Assert.Equal((int)SalePaymentStatusOption.Paid, completedSale.SalePaymentStatusId);
        Assert.Equal((int)SaleStatusOption.Completed, completedSale.SaleStatusId);
    }

    [Fact]
    public async Task Employee_CanCreateAndSendDelivery()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        var location = await _factory.SeedDeliveryLocationAsync();
        using var employee = await CreateEmployeeClientAsync();

        var sale = await employee.PostAsJsonAsync("/api/v1/sales", new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            ProductVariants = [new CreateSaleProductDTO { ProductId = productVariant.ProductVariantId, Quantity = 1 }]
        });
        var saleId = await sale.Content.ReadFromJsonAsync<int>();
        var delivery = await employee.PostAsJsonAsync($"/api/v1/sales/{saleId}/deliveries", new CreateSaleDeliveryDTO
        {
            Code = $"EMP-{Guid.NewGuid():N}"[..20],
            MunicipalityId = location.MunicipalityId,
            DeliveryAgencyId = location.DeliveryAgencyId,
            ShippingChargedToClient = 50m
        });
        var deliveryId = await delivery.Content.ReadFromJsonAsync<int>();

        var send = await employee.PostAsync($"/api/v1/sales/{saleId}/deliveries/{deliveryId}/send", null);

        Assert.Equal(HttpStatusCode.Created, sale.StatusCode);
        Assert.Equal(HttpStatusCode.Created, delivery.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, send.StatusCode);
    }

    [Fact]
    public async Task Reconciliation_RejectsPartialCashOnDeliveryCollection()
    {
        var productVariant = await _factory.SeedProductAsync(quantity: 1, receivedQuantity: 1, availableQuantity: 1, salePrice: 500m);
        var location = await _factory.SeedDeliveryLocationAsync();
        using var admin = await CreateAdminClientAsync();

        var sale = await admin.PostAsJsonAsync("/api/v1/sales", new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            ProductVariants = [new CreateSaleProductDTO { ProductId = productVariant.ProductVariantId, Quantity = 1 }]
        });
        var saleId = await sale.Content.ReadFromJsonAsync<int>();
        var delivery = await admin.PostAsJsonAsync($"/api/v1/sales/{saleId}/deliveries", new CreateSaleDeliveryDTO
        {
            Code = $"PARTIAL-{Guid.NewGuid():N}"[..20],
            MunicipalityId = location.MunicipalityId,
            DeliveryAgencyId = location.DeliveryAgencyId,
            ShippingChargedToClient = 50m
        });
        var deliveryId = await delivery.Content.ReadFromJsonAsync<int>();
        await admin.PostAsync($"/api/v1/sales/{saleId}/deliveries/{deliveryId}/send", null);

        var reconciliation = await admin.PostAsJsonAsync("/api/v1/deliveryagencyreconciliations", new CreateDeliveryAgencyReconciliationDTO
        {
            DeliveryAgencyId = location.DeliveryAgencyId,
            SettlementExchangeRate = 36m,
            Deliveries = [new ReconcileSaleDeliveryDTO { SaleDeliveryId = deliveryId, AmountCollectedNio = 200m }]
        });

        Assert.Equal(HttpStatusCode.BadRequest, reconciliation.StatusCode);
    }

    [Fact]
    public async Task Employee_CannotRefundCancelOrManagePostSaleOperations()
    {
        using var employee = await CreateEmployeeClientAsync();

        var refund = await employee.PostAsJsonAsync("/api/v1/sales/1/payment-movements/1/refunds", new RefundSalePaymentMovementDTO());
        var cancel = await employee.PostAsync("/api/v1/sales/1/cancel", content: null);
        var returnRequest = await employee.PostAsJsonAsync("/api/v1/sales/1/returns", new CreateSaleReturnDTO());
        var exchange = await employee.PostAsJsonAsync("/api/v1/sales/1/exchanges", new CreateSaleExchangeDTO());
        var reconciliation = await employee.PostAsJsonAsync("/api/v1/deliveryagencyreconciliations", new { });

        Assert.Equal(HttpStatusCode.Forbidden, refund.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, returnRequest.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, exchange.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reconciliation.StatusCode);
    }

    private static Task<HttpResponseMessage> ReceiveAsync(HttpClient client, int orderId, int productId, int quantity)
        => client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts", new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 0,
            ProductVariants = [new ReceiveOrderProductDTO { ProductId = productId, Quantity = quantity, Weight = 1 }]
        });

    private static async Task<int> CreateSaleAsync(HttpClient client, int productId, decimal initialPayment)
    {
        var response = await client.PostAsJsonAsync("/api/v1/sales", new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.InStoreSale,
            ProductVariants = [new CreateSaleProductDTO { ProductId = productId, Quantity = 1 }],
            PaymentMovements = [new CreateSalePaymentMovementDTO { PaymentMethodId = (int)PaymentMethodOption.Cash, ProductAmount = initialPayment }]
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    private static async Task<SaleDTO> GetSaleAsync(HttpClient client, int saleId)
    {
        var response = await client.GetAsync($"/api/v1/sales/{saleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SaleDTO>())!;
    }

    private static async Task<List<SaleReturnDTO>> GetReturnsAsync(HttpClient client, int saleId)
    {
        var response = await client.GetAsync($"/api/v1/sales/{saleId}/returns");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<SaleReturnDTO>>())!;
    }

    private static async Task<List<SaleExchangeDTO>> GetExchangesAsync(HttpClient client, int saleId)
    {
        var response = await client.GetAsync($"/api/v1/sales/{saleId}/exchanges");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<SaleExchangeDTO>>())!;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
        => await CreateAuthenticatedClientAsync(PrettyWomanApiFactory.AdminUsername, PrettyWomanApiFactory.AdminPassword);

    private async Task<HttpClient> CreateEmployeeClientAsync()
    {
        await _factory.EnsureEmployeeAsync();
        return await CreateAuthenticatedClientAsync(PrettyWomanApiFactory.EmployeeEmail, PrettyWomanApiFactory.EmployeePassword);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO { Username = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
