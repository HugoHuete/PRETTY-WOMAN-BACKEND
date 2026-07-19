using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Mappings;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Orders;

public class OrderServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(config =>
    {
        config.AddProfile<OrdersProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();


    [Fact]
    public async Task CloseShortagesAsync_ClosesOrderAndRegistersSupplierRefund()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO-FALTANTE", "Blusa faltante"));
        var order = await context.Orders.Include(item => item.Products).SingleAsync(item => item.Id == orderId);
        var product = order.Products.Single();
        product.ReceivedQuantity = 1;
        product.AvailableQuantity = 1;
        order.OrderStatusId = (int)OrderStatusCode.PartiallyReceived;
        await context.SaveChangesAsync();

        var closedOrder = await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = product.Id }]
        });

        Assert.Equal((int)OrderStatusCode.PendingRefund, closedOrder.OrderStatusId);
        Assert.Single(closedOrder.PurchaseShortages);
        Assert.Equal(1, closedOrder.PurchaseShortages.Single().Quantity);
        Assert.Equal(292m, closedOrder.TotalShortageLossNio);
        Assert.Equal(292m, closedOrder.MerchandiseTotalNio);
        Assert.Equal(8m, closedOrder.AmountUsd);
        Assert.Equal(292m, closedOrder.ReceivedAmountNio);
        Assert.Equal(3942m, closedOrder.TotalCostNio);
        Assert.Equal(PurchaseShortageRefundStatusOption.PendingRefund, closedOrder.PurchaseShortages.Single().RefundStatus);

        var refundedOrder = await service.CreateSupplierRefundAsync(orderId, new CreateSupplierRefundDTO
        {
            AmountNio = 200m,
            Reference = "CR-001"
        });

        Assert.Equal(200m, refundedOrder.TotalSupplierRefundNio);
        Assert.Equal(92m, refundedOrder.NetShortageLossNio);
        Assert.Equal(PurchaseShortageRefundStatusOption.PartiallyRefunded, refundedOrder.PurchaseShortages.Single().RefundStatus);
        Assert.Equal((int)OrderStatusCode.Received, refundedOrder.OrderStatusId);
        var movement = await context.FinancialMovements.SingleAsync(item => item.FinancialMovementTypeId == (int)FinancialMovementTypeOption.SupplierRefund);
        Assert.Equal((int)MovementDirectionOptions.In, movement.MovementDirectionId);
        Assert.Equal(200m, movement.Amount);
    }

    [Fact]
    public async Task CloseShortagesAsync_RegistersZeroLossShortageForZeroCostProduct()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO-SIN-COSTO", "Muestra sin costo"));
        var order = await context.Orders.Include(item => item.Products).SingleAsync(item => item.Id == orderId);
        var product = order.Products.Single();
        product.MerchandiseTotalCostNio = 0;
        product.AllocatedShippingCostNio = 0;
        product.TotalCostNio = 0;
        product.UnitCostNio = 0;
        product.UnitCostUsd = 0;
        product.ReceivedQuantity = 1;
        product.AvailableQuantity = 1;
        order.OrderStatusId = (int)OrderStatusCode.PartiallyReceived;
        await context.SaveChangesAsync();

        var closedOrder = await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = product.Id }]
        });

        var shortage = Assert.Single(closedOrder.PurchaseShortages);
        Assert.Equal(0m, shortage.LossAmountNio);
        Assert.Equal(0m, closedOrder.TotalShortageLossNio);
        Assert.Equal(0m, closedOrder.NetShortageLossNio);
        Assert.Equal((int)OrderStatusCode.Received, closedOrder.OrderStatusId);
    }

    [Fact]
    public async Task DeclineSupplierRefundAsync_MarksShortagesAsNotRefundedWithoutFinancialMovement()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO-SIN-REEMBOLSO", "Blusa sin crédito"));
        var order = await context.Orders.Include(item => item.Products).SingleAsync(item => item.Id == orderId);
        var product = order.Products.Single();
        product.ReceivedQuantity = 1;
        product.AvailableQuantity = 1;
        order.OrderStatusId = (int)OrderStatusCode.PartiallyReceived;
        await context.SaveChangesAsync();
        await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = product.Id }]
        });

        var declinedOrder = await service.DeclineSupplierRefundAsync(orderId, new DeclineSupplierRefundDTO
        {
            Comments = "Proveedor no emitirá crédito."
        });

        Assert.Null(declinedOrder.SupplierRefund);
        Assert.NotNull(declinedOrder.SupplierRefundDeclinedAt);
        Assert.Equal("Proveedor no emitirá crédito.", declinedOrder.SupplierRefundDeclineComments);
        Assert.Equal(PurchaseShortageRefundStatusOption.NotRefunded, declinedOrder.PurchaseShortages.Single().RefundStatus);
        Assert.Equal((int)OrderStatusCode.Received, declinedOrder.OrderStatusId);
        Assert.Empty(await context.FinancialMovements.Where(item => item.FinancialMovementTypeId == (int)FinancialMovementTypeOption.SupplierRefund).ToListAsync());
        await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateSupplierRefundAsync(orderId, new CreateSupplierRefundDTO { AmountNio = 1m }));
    }

    [Fact]
    public async Task CloseShortagesAsync_ClosesFullyMissingVariant()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO-AUSENTE", "Variante no recibida"));
        var product = await context.Products.SingleAsync(item => item.OrderId == orderId);

        var closedOrder = await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = product.Id }]
        });

        Assert.Equal((int)OrderStatusCode.PendingRefund, closedOrder.OrderStatusId);
        Assert.Equal(2, Assert.Single(closedOrder.PurchaseShortages).Quantity);
        Assert.Equal(584m, closedOrder.TotalShortageLossNio);
        Assert.Equal(0, (await context.Products.SingleAsync(item => item.Id == product.Id)).Quantity);
    }

    [Fact]
    public async Task CloseShortagesAsync_ConservesMerchandiseCentsWhenSplittingLoss()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO-CENTAVOS", "Producto con centavos"));
        var order = await context.Orders.Include(item => item.Products).SingleAsync(item => item.Id == orderId);
        var product = order.Products.Single();
        product.MerchandiseTotalCostNio = 0.03m;
        product.TotalCostNio = product.MerchandiseTotalCostNio + product.AllocatedShippingCostNio;
        product.ReceivedQuantity = 1;
        product.AvailableQuantity = 1;
        order.OrderStatusId = (int)OrderStatusCode.PartiallyReceived;
        await context.SaveChangesAsync();

        var closedOrder = await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = product.Id }]
        });

        Assert.Equal(0.02m, closedOrder.TotalShortageLossNio);
        Assert.Equal(0.01m, closedOrder.MerchandiseTotalNio);
        Assert.Equal(0.03m, closedOrder.TotalShortageLossNio + closedOrder.MerchandiseTotalNio);
    }

    [Fact]
    public async Task CreateAsync_ConvertsSupplierShippingUsdToNioForLocalPurchase()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);

        var orderId = await service.CreateAsync(new CreateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Nio,
            SupplierShippingCostUsd = 10m,
            ProductDetails =
            [
                new CreateOrderProductDetailDTO
                {
                    SupplierProductCode = "LOCAL-001",
                    Name = "Blusa local",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Color = "Negro",
                            Quantity = 2,
                            UnitCost = 250m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        });

        var order = await context.Orders.SingleAsync(order => order.Id == orderId);
        var product = await context.Products.SingleAsync();

        Assert.Equal((int)PurchaseCurrencyOption.Nio, order.PurchaseCurrencyId);
        Assert.Equal(36.5m, order.ExchangeRate);
        Assert.Equal(13.70m, order.AmountUsd);
        Assert.Equal(500m, order.MerchandiseTotalNio);
        Assert.Equal(865m, order.TotalCostNio);
        Assert.Equal(11.85m, product.UnitCostUsd);
        Assert.Equal(432.5m, product.UnitCostNio);
    }

    [Fact]
    public async Task CreateAsync_UsesProvidedPurchaseDate()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var purchaseDate = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var request = CreateOrderRequest("SOHO25119", "Blusa satin");
        request.PurchaseDate = purchaseDate;

        var orderId = await service.CreateAsync(request);

        var order = await context.Orders.SingleAsync(order => order.Id == orderId);
        Assert.Equal(purchaseDate, order.PurchaseDate);
        var financialMovement = await context.FinancialMovements.SingleAsync(movement => movement.OrderId == orderId);
        Assert.Equal(purchaseDate, financialMovement.MovementDate);
    }


    [Fact]
    public async Task CreateAsync_InterpretsUnspecifiedPurchaseDateAsBusinessLocalTime()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var purchaseDate = new DateTime(2025, 11, 24, 0, 0, 0, DateTimeKind.Unspecified);
        var request = CreateOrderRequest("SOHO25118", "Vestido casual");
        request.PurchaseDate = purchaseDate;

        var orderId = await service.CreateAsync(request);

        var expectedDate = new DateTime(2025, 11, 24, 6, 0, 0, DateTimeKind.Utc);
        var order = await context.Orders.SingleAsync(order => order.Id == orderId);
        var financialMovement = await context.FinancialMovements.SingleAsync(movement => movement.OrderId == orderId);

        Assert.Equal(DateTimeKind.Utc, order.PurchaseDate.Kind);
        Assert.Equal(expectedDate, order.PurchaseDate);
        Assert.Equal(DateTimeKind.Utc, financialMovement.MovementDate.Kind);
        Assert.Equal(expectedDate, financialMovement.MovementDate);
    }
    [Fact]
    public async Task CreateAsync_AllowsOrderWithoutProducts()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);

        var orderId = await service.CreateAsync(new CreateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 0m,
            Comments = "Compra pendiente de detalle"
        });

        var order = await context.Orders
            .Include(order => order.Products)
            .SingleAsync(order => order.Id == orderId);

        Assert.Empty(order.Products);
        Assert.Equal(0m, order.AmountUsd);
        Assert.Equal(0m, order.MerchandiseTotalNio);
        Assert.Equal(0m, order.TotalCostNio);
        Assert.False(await context.FinancialMovements.AnyAsync(movement => movement.OrderId == orderId));
    }

    [Fact]
    public async Task UpdateAsync_AddsProductsToOrderCreatedWithoutProducts()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);

        var orderId = await service.CreateAsync(new CreateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 0m
        });
        await service.UpdateAsync(orderId, new UpdateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 0m,
            ProductDetails =
            [
                new CreateOrderProductDetailDTO
                {
                    SupplierProductCode = "SOHO25120",
                    Name = "Pantalon cargo",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Color = "Azul",
                            Quantity = 2,
                            UnitCost = 8m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        });

        var order = await context.Orders
            .Include(order => order.Products)
            .SingleAsync(order => order.Id == orderId);
        var financialMovement = await context.FinancialMovements.SingleAsync(movement => movement.OrderId == orderId);

        Assert.Single(order.Products);
        Assert.Equal(584m, order.TotalCostNio);
        Assert.Equal(order.TotalCostNio, financialMovement.Amount);
    }


    [Fact]
    public async Task UpdateAsync_CreatesSupplierPaymentMovementWithOriginalOrderDate()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var purchaseDate = new DateTime(2026, 7, 5, 14, 0, 0, DateTimeKind.Utc);

        var orderId = await service.CreateAsync(new CreateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 0m,
            PurchaseDate = purchaseDate
        });

        await service.UpdateAsync(orderId, new UpdateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 0m,
            ProductDetails =
            [
                new CreateOrderProductDetailDTO
                {
                    SupplierProductCode = "SOHO25120",
                    Name = "Pantalon cargo",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Color = "Azul",
                            Quantity = 2,
                            UnitCost = 8m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        });

        var financialMovement = await context.FinancialMovements.SingleAsync(movement => movement.OrderId == orderId);

        Assert.Equal(purchaseDate, financialMovement.MovementDate);
    }
    [Fact]
    public async Task UpdateAsync_ReusesProductDetailCodeWhenProductDetailIdIsProvided()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);

        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO25120", "Pantalon cargo"));
        var existingProductDetail = await context.ProductDetails.SingleAsync();

        await service.UpdateAsync(orderId, new UpdateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 150m,
            ProductDetails =
            [
                new CreateOrderProductDetailDTO
                {
                    Id = existingProductDetail.Id,
                    SupplierProductCode = "SOHO25120-CORREGIDO",
                    Name = "Pantalon cargo corregido",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Color = "Azul",
                            Quantity = 3,
                            UnitCost = 9m,
                            SalePrice = 650m
                        }
                    ]
                }
            ]
        });

        var updatedProductDetail = await context.ProductDetails.SingleAsync();

        Assert.Equal(existingProductDetail.Id, updatedProductDetail.Id);
        Assert.Equal(existingProductDetail.Code, updatedProductDetail.Code);
        Assert.Equal("SOHO25120-CORREGIDO", updatedProductDetail.SupplierProductCode);
        Assert.Equal("Pantalon cargo corregido", updatedProductDetail.Name);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenProductDetailIdDoesNotBelongToOrder()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);

        var firstOrderId = await service.CreateAsync(CreateOrderRequest("SOHO25120", "Pantalon cargo"));
        await service.CreateAsync(CreateOrderRequest("SOHO25121", "Blusa satin"));
        var otherOrderProductDetail = await context.ProductDetails
            .Where(productDetail => productDetail.SupplierProductCode == "SOHO25121")
            .SingleAsync();

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.UpdateAsync(firstOrderId, new UpdateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 100m,
            ProductDetails =
            [
                new CreateOrderProductDetailDTO
                {
                    Id = otherOrderProductDetail.Id,
                    SupplierProductCode = "SOHO25120",
                    Name = "Pantalon cargo",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Color = "Azul",
                            Quantity = 1,
                            UnitCost = 8m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        }));

        Assert.Equal($"El producto detalle con id '{otherOrderProductDetail.Id}' no pertenece a la orden.", exception.Message);
    }

    [Fact]
    public async Task AddTrackingNumbersAsync_RejectsReceiptFromAnotherOrder()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.ShippingCompanies.Add(new ShippingCompany { Id = 1, Name = "Cargo Express" });
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO25122", "Vestido"));
        var otherOrderId = await service.CreateAsync(CreateOrderRequest("SOHO25123", "Blusa"));
        var receipt = new ProductReceipt { OrderId = otherOrderId, ReceivedDate = DateTime.UtcNow };
        context.ProductReceipts.Add(receipt);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.AddTrackingNumbersAsync(orderId,
        [
            new CreateOrderTrackingNumberDTO
            {
                ShippingCompanyId = 1,
                TrackingNumber = "TRACK-OTHER-ORDER",
                ProductReceiptId = receipt.Id
            }
        ]));

        Assert.Equal($"La recepción de productos con id '{receipt.Id}' no pertenece a la orden '{orderId}'.", exception.Message);
    }

    private static OrderService CreateService(ApplicationDbContext context)
    {
        return new OrderService(context, Mapper);
    }

    private static CreateOrderDTO CreateOrderRequest(string supplierProductCode, string name)
    {
        return new CreateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 100m,
            ProductDetails =
            [
                new CreateOrderProductDetailDTO
                {
                    SupplierProductCode = supplierProductCode,
                    Name = name,
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Color = "Azul",
                            Quantity = 2,
                            UnitCost = 8m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        };
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext context)
    {
        context.Suppliers.Add(new Supplier { Id = 1, Name = "SOHO" });
        context.Categories.Add(new Category { Id = 1, Name = "Ropa" });
        context.Subcategories.Add(new Subcategory { Id = 1, CategoryId = 1, Name = "Pantalones" });
        context.SizeGroups.Add(new SizeGroup { Id = 1, Name = "Regular" });
        context.Sizes.Add(new Size { Id = 1, Name = "S", SizeGroupId = 1, DisplayOrder = 1 });
        context.OrderStatuses.Add(new OrderStatus { Id = (int)OrderStatusCode.Pending, Name = "Pending" });
        context.OrderStatuses.Add(new OrderStatus { Id = (int)OrderStatusCode.PartiallyReceived, Name = "PartiallyReceived" });
        context.OrderStatuses.Add(new OrderStatus { Id = (int)OrderStatusCode.Received, Name = "Received" });
        context.OrderStatuses.Add(new OrderStatus { Id = (int)OrderStatusCode.PendingRefund, Name = "PendingRefund" });
        context.MovementDirections.Add(new MovementDirection { Id = (int)MovementDirectionOptions.Out, Name = "Out" });
        context.MovementDirections.Add(new MovementDirection { Id = (int)MovementDirectionOptions.In, Name = "In" });
        context.FinancialMovementTypes.Add(new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SupplierPayment, Name = "SupplierPayment" });
        context.FinancialMovementTypes.Add(new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SupplierRefund, Name = "SupplierRefund" });
        context.DollarExchangeRates.Add(new DollarExchangeRate
        {
            Id = 1,
            BankRate = 36.5m,
            StoreRate = 37m,
            StartDate = new DateTime(2026, 1, 1),
            Enabled = true
        });
        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
