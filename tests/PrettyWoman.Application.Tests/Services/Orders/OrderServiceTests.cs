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
    public async Task GetAllAsync_PaginatesAndFiltersByPurchaseDateStatusAndSupplier()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var olderMatchingOrderId = await service.CreateAsync(CreateOrderRequest("SOHO-OLD", "Blusa anterior"));
        var newerMatchingOrderId = await service.CreateAsync(CreateOrderRequest("SOHO-NEW", "Blusa nueva"));
        var outsideDateOrderId = await service.CreateAsync(CreateOrderRequest("SOHO-OUT-DATE", "Blusa fuera de fecha"));
        var otherStatusOrderId = await service.CreateAsync(CreateOrderRequest("SOHO-STATUS", "Blusa recibida"));
        var otherSupplierOrderId = await service.CreateAsync(CreateOrderRequest("SOHO-SUPPLIER", "Blusa proveedor"));

        var olderMatchingOrder = await context.Orders.FindAsync(olderMatchingOrderId);
        olderMatchingOrder!.PurchaseDate = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        olderMatchingOrder.OrderStatusId = (int)OrderStatusCode.Pending;
        olderMatchingOrder.SupplierId = 1;

        var newerMatchingOrder = await context.Orders.FindAsync(newerMatchingOrderId);
        newerMatchingOrder!.PurchaseDate = new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc);
        newerMatchingOrder.OrderStatusId = (int)OrderStatusCode.Pending;
        newerMatchingOrder.SupplierId = 1;

        var outsideDateOrder = await context.Orders.FindAsync(outsideDateOrderId);
        outsideDateOrder!.PurchaseDate = new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc);
        outsideDateOrder.OrderStatusId = (int)OrderStatusCode.Pending;
        outsideDateOrder.SupplierId = 1;

        var otherStatusOrder = await context.Orders.FindAsync(otherStatusOrderId);
        otherStatusOrder!.PurchaseDate = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        otherStatusOrder.OrderStatusId = (int)OrderStatusCode.Received;
        otherStatusOrder.SupplierId = 1;

        var otherSupplierOrder = await context.Orders.FindAsync(otherSupplierOrderId);
        otherSupplierOrder!.PurchaseDate = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        otherSupplierOrder.OrderStatusId = (int)OrderStatusCode.Pending;
        otherSupplierOrder.SupplierId = 2;
        await context.SaveChangesAsync();

        var result = await service.GetAllAsync(new OrderQueryDTO
        {
            Page = 2,
            PageSize = 1,
            OrderStatusId = (int)OrderStatusCode.Pending,
            SupplierId = 1,
            PurchaseDateFrom = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            PurchaseDateTo = new DateTime(2026, 7, 12, 23, 59, 59, DateTimeKind.Utc)
        });

        var order = Assert.Single(result.Items);
        Assert.Equal(olderMatchingOrderId, order.Id);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task CloseShortagesAsync_ClosesOrderAndRegistersSupplierRefund()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO-FALTANTE", "Blusa faltante"));
        var order = await context.Orders.Include(item => item.ProductVariants).SingleAsync(item => item.Id == orderId);
        var productVariant = order.ProductVariants.Single();
        order.Comments = "Compra parcial confirmada con el proveedor.";
        productVariant.ReceivedQuantity = 1;
        productVariant.AvailableQuantity = 1;
        order.OrderStatusId = (int)OrderStatusCode.PartiallyReceived;
        await context.SaveChangesAsync();

        var closedOrder = await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = productVariant.Id }]
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
        Assert.Equal("Compra parcial confirmada con el proveedor.", refundedOrder.Comments);
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
        var order = await context.Orders.Include(item => item.ProductVariants).SingleAsync(item => item.Id == orderId);
        var productVariant = order.ProductVariants.Single();
        productVariant.MerchandiseTotalCostNio = 0;
        productVariant.AllocatedShippingCostNio = 0;
        productVariant.TotalCostNio = 0;
        productVariant.UnitCostNio = 0;
        productVariant.UnitCostUsd = 0;
        productVariant.ReceivedQuantity = 1;
        productVariant.AvailableQuantity = 1;
        order.OrderStatusId = (int)OrderStatusCode.PartiallyReceived;
        await context.SaveChangesAsync();

        var closedOrder = await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = productVariant.Id }]
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
        var order = await context.Orders.Include(item => item.ProductVariants).SingleAsync(item => item.Id == orderId);
        var productVariant = order.ProductVariants.Single();
        productVariant.ReceivedQuantity = 1;
        productVariant.AvailableQuantity = 1;
        order.OrderStatusId = (int)OrderStatusCode.PartiallyReceived;
        await context.SaveChangesAsync();
        await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = productVariant.Id }]
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
        var productVariant = await context.ProductVariants.SingleAsync(item => item.OrderId == orderId);

        var closedOrder = await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = productVariant.Id }]
        });

        Assert.Equal((int)OrderStatusCode.PendingRefund, closedOrder.OrderStatusId);
        Assert.Equal(2, Assert.Single(closedOrder.PurchaseShortages).Quantity);
        Assert.Equal(584m, closedOrder.TotalShortageLossNio);
        Assert.Equal(0, (await context.ProductVariants.SingleAsync(item => item.Id == productVariant.Id)).Quantity);
    }

    [Fact]
    public async Task CloseShortagesAsync_ConservesMerchandiseCentsWhenSplittingLoss()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO-CENTAVOS", "Producto con centavos"));
        var order = await context.Orders.Include(item => item.ProductVariants).SingleAsync(item => item.Id == orderId);
        var productVariant = order.ProductVariants.Single();
        productVariant.MerchandiseTotalCostNio = 0.03m;
        productVariant.TotalCostNio = productVariant.MerchandiseTotalCostNio + productVariant.AllocatedShippingCostNio;
        productVariant.ReceivedQuantity = 1;
        productVariant.AvailableQuantity = 1;
        order.OrderStatusId = (int)OrderStatusCode.PartiallyReceived;
        await context.SaveChangesAsync();

        var closedOrder = await service.CloseShortagesAsync(orderId, new CloseOrderShortagesDTO
        {
            Items = [new CloseOrderShortageItemDTO { ProductId = productVariant.Id }]
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
            Products =
            [
                new CreateOrderProductDTO
                {
                    SupplierProductCode = "LOCAL-001",
                    Name = "Blusa local",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Variant = "Negro",
                            Quantity = 2,
                            UnitCost = 250m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        });

        var order = await context.Orders.SingleAsync(order => order.Id == orderId);
        var productVariant = await context.ProductVariants.SingleAsync();

        Assert.Equal((int)PurchaseCurrencyOption.Nio, order.PurchaseCurrencyId);
        Assert.Equal(36.5m, order.ExchangeRate);
        Assert.Equal(13.70m, order.AmountUsd);
        Assert.Equal(500m, order.MerchandiseTotalNio);
        Assert.Equal(865m, order.TotalCostNio);
        Assert.Equal(11.85m, productVariant.UnitCostUsd);
        Assert.Equal(432.5m, productVariant.UnitCostNio);
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
            .Include(order => order.ProductVariants)
            .SingleAsync(order => order.Id == orderId);

        Assert.Empty(order.ProductVariants);
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
            Products =
            [
                new CreateOrderProductDTO
                {
                    SupplierProductCode = "SOHO25120",
                    Name = "Pantalon cargo",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Variant = "Azul",
                            Quantity = 2,
                            UnitCost = 8m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        });

        var order = await context.Orders
            .Include(order => order.ProductVariants)
            .SingleAsync(order => order.Id == orderId);
        var financialMovement = await context.FinancialMovements.SingleAsync(movement => movement.OrderId == orderId);

        Assert.Single(order.ProductVariants);
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
            Products =
            [
                new CreateOrderProductDTO
                {
                    SupplierProductCode = "SOHO25120",
                    Name = "Pantalon cargo",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Variant = "Azul",
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
    public async Task UpdateAsync_ReusesProductCodeWhenProductIdIsProvided()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);

        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO25120", "Pantalon cargo"));
        var existingProduct = await context.Products.SingleAsync();

        await service.UpdateAsync(orderId, new UpdateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 150m,
            Products =
            [
                new CreateOrderProductDTO
                {
                    Id = existingProduct.Id,
                    SupplierProductCode = "SOHO25120-CORREGIDO",
                    Name = "Pantalon cargo corregido",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Variant = "Azul",
                            Quantity = 3,
                            UnitCost = 9m,
                            SalePrice = 650m
                        }
                    ]
                }
            ]
        });

        var updatedProduct = await context.Products.SingleAsync();

        Assert.Equal(existingProduct.Id, updatedProduct.Id);
        Assert.Equal(existingProduct.Code, updatedProduct.Code);
        Assert.Equal("SOHO25120-CORREGIDO", updatedProduct.SupplierProductCode);
        Assert.Equal("Pantalon cargo corregido", updatedProduct.Name);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenProductIdDoesNotBelongToOrder()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = CreateService(context);

        var firstOrderId = await service.CreateAsync(CreateOrderRequest("SOHO25120", "Pantalon cargo"));
        await service.CreateAsync(CreateOrderRequest("SOHO25121", "Blusa satin"));
        var otherOrderProduct = await context.Products
            .Where(product => product.SupplierProductCode == "SOHO25121")
            .SingleAsync();

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.UpdateAsync(firstOrderId, new UpdateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 100m,
            Products =
            [
                new CreateOrderProductDTO
                {
                    Id = otherOrderProduct.Id,
                    SupplierProductCode = "SOHO25120",
                    Name = "Pantalon cargo",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Variant = "Azul",
                            Quantity = 1,
                            UnitCost = 8m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        }));

        Assert.Equal($"El producto con id '{otherOrderProduct.Id}' no pertenece a la orden.", exception.Message);
    }

    [Fact]
    public async Task GetTrackingNumbersAsync_FiltersByReceiptStatus()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.ShippingCompanies.Add(new ShippingCompany { Id = 1, Name = "Cargo Express" });
        var service = CreateService(context);
        var orderId = await service.CreateAsync(CreateOrderRequest("SOHO-TRACKINGS", "Vestido"));
        var receipt = new ProductReceipt { OrderId = orderId, ReceivedDate = DateTime.UtcNow };
        context.ProductReceipts.Add(receipt);
        await context.SaveChangesAsync();
        context.OrderTrackingNumbers.AddRange(
            new OrderTrackingNumber
            {
                OrderId = orderId,
                ShippingCompanyId = 1,
                TrackingNumber = "TRACK-RECEIVED",
                ProductReceiptId = receipt.Id
            },
            new OrderTrackingNumber
            {
                OrderId = orderId,
                ShippingCompanyId = 1,
                TrackingNumber = "TRACK-PENDING"
            });
        await context.SaveChangesAsync();

        var all = (await service.GetTrackingNumbersAsync(orderId)).ToList();
        var received = (await service.GetTrackingNumbersAsync(orderId, isReceived: true)).ToList();
        var pending = (await service.GetTrackingNumbersAsync(orderId, isReceived: false)).ToList();

        Assert.Equal(2, all.Count);
        Assert.Equal("TRACK-RECEIVED", Assert.Single(received).TrackingNumber);
        Assert.Equal("TRACK-PENDING", Assert.Single(pending).TrackingNumber);
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
            Products =
            [
                new CreateOrderProductDTO
                {
                    SupplierProductCode = supplierProductCode,
                    Name = name,
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Variant = "Azul",
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
        context.Suppliers.Add(new Supplier { Id = 2, Name = "SHEIN" });
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
