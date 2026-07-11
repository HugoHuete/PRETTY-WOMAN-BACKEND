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

public class OrderReceiptServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(config =>
    {
        config.AddProfile<OrdersProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task ReceiveAsync_ReceivesProductsAndRegistersDirectWarehouseShippingCost()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = new OrderReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 4));
        var product = await context.Products.SingleAsync();

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 2
                }
            ]
        });

        var order = await context.Orders.SingleAsync(order => order.Id == orderId);
        product = await context.Products.SingleAsync(storedProduct => storedProduct.Id == product.Id);
        var productReceipt = await context.ProductReceipts.SingleAsync();
        var receiptDetail = await context.ProductReceiptDetails.SingleAsync();
        var inventoryMovement = await context.InventoryMovements.SingleAsync();
        var warehouseShippingMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.WarehouseShippingPayment);

        Assert.Equal(productReceipt.Id, receipt.Id);
        Assert.Equal(orderId, receipt.OrderId);
        Assert.Equal(orderId, productReceipt.OrderId);
        Assert.Equal(10m, receipt.WarehouseShippingCostUsd);
        Assert.Equal(365m, receipt.WarehouseShippingCostNio);
        Assert.Equal((int)OrderStatusCode.PartiallyReceived, receipt.OrderStatusId);
        Assert.Equal(584m, order.ReceivedAmountNio);
        Assert.Equal(10m, order.WarehouseShippingCostUsd);
        Assert.Equal(2, product.ReceivedQuantity);
        Assert.Equal(2, product.AvailableQuantity);
        Assert.Equal(730m, product.AllocatedShippingCostNio);
        Assert.Equal(13m, product.UnitCostUsd);
        Assert.Equal(product.Id, receiptDetail.ProductId);
        Assert.Equal(2m, receiptDetail.Quantity);
        Assert.Equal((int)InventoryMovementTypeOption.PurchaseReceived, inventoryMovement.InventoryMovementTypeId);
        Assert.Equal((int)InventoryStockBucketOption.External, inventoryMovement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Available, inventoryMovement.ToStockBucketId);
        Assert.Equal(2, inventoryMovement.Quantity);
        Assert.Equal(365m, warehouseShippingMovement.Amount);
    }


    [Fact]
    public async Task ReceiveAsync_InterpretsUnspecifiedReceivedDateAsBusinessLocalTime()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = new OrderReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();
        var receivedDate = new DateTime(2025, 11, 24, 0, 0, 0, DateTimeKind.Unspecified);

        await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            ReceivedDate = receivedDate,
            WarehouseShippingCostUsd = 10m,
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1
                }
            ]
        });

        var expectedDate = new DateTime(2025, 11, 24, 6, 0, 0, DateTimeKind.Utc);
        var productReceipt = await context.ProductReceipts.SingleAsync();
        var inventoryMovement = await context.InventoryMovements.SingleAsync();
        var warehouseShippingMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.WarehouseShippingPayment);

        Assert.Equal(DateTimeKind.Utc, productReceipt.ReceivedDate.Kind);
        Assert.Equal(expectedDate, productReceipt.ReceivedDate);
        Assert.Equal(DateTimeKind.Utc, inventoryMovement.MovementDate.Kind);
        Assert.Equal(expectedDate, inventoryMovement.MovementDate);
        Assert.Equal(DateTimeKind.Utc, warehouseShippingMovement.MovementDate.Kind);
        Assert.Equal(expectedDate, warehouseShippingMovement.MovementDate);
    }
    [Fact]
    public async Task ReceiveAsync_UsesTrackingShippingCostWhenOrderHasTrackingNumbers()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = new OrderReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        context.OrderTrackingNumbers.Add(new OrderTrackingNumber
        {
            OrderId = orderId,
            ShippingCompanyId = 1,
            TrackingNumber = "1Z999"
        });
        await context.SaveChangesAsync();
        var tracking = await context.OrderTrackingNumbers.SingleAsync();

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            TrackingNumbers =
            [
                new ReceiveOrderTrackingNumberDTO
                {
                    Id = tracking.Id,
                    Weight = 8.5m,
                    ShippingCostUsd = 12m
                }
            ],
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 2
                }
            ]
        });

        var order = await context.Orders.SingleAsync(order => order.Id == orderId);
        tracking = await context.OrderTrackingNumbers.SingleAsync(storedTracking => storedTracking.Id == tracking.Id);

        Assert.Equal(12m, receipt.WarehouseShippingCostUsd);
        Assert.Equal(438m, receipt.WarehouseShippingCostNio);
        Assert.Equal((int)OrderStatusCode.Received, order.OrderStatusId);
        Assert.Equal(order.MerchandiseTotalNio, order.ReceivedAmountNio);
        Assert.Equal(12m, order.WarehouseShippingCostUsd);
        Assert.Equal(8.5m, tracking.Weight);
        product = await context.Products.SingleAsync(storedProduct => storedProduct.Id == product.Id);

        Assert.Equal(12m, tracking.ShippingCost);
        Assert.Equal(19m, product.UnitCostUsd);
        Assert.Equal(receipt.Id, tracking.ProductReceiptId);
    }

    [Fact]
    public async Task ReceiveAsync_RejectsDirectWarehouseShippingCostWhenOrderHasTrackingNumbers()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = new OrderReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        context.OrderTrackingNumbers.Add(new OrderTrackingNumber
        {
            OrderId = orderId,
            ShippingCompanyId = 1,
            TrackingNumber = "1Z999"
        });
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1
                }
            ]
        }));

        Assert.Contains("tracking", exception.Message);
    }

    [Fact]
    public async Task ReceiveAsync_RejectsMissingTrackingCostsWhenOrderHasTrackingNumbers()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = new OrderReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        context.OrderTrackingNumbers.Add(new OrderTrackingNumber
        {
            OrderId = orderId,
            ShippingCompanyId = 1,
            TrackingNumber = "1Z999"
        });
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1
                }
            ]
        }));

        Assert.Contains("Debe enviar al menos un tracking", exception.Message);
    }

    [Fact]
    public async Task ReceiveAsync_AllocatesWarehouseShippingByEstimatedProductWeight()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = new OrderReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateTwoProductOrderRequest());
        var products = await context.Products.OrderBy(product => product.Id).ToListAsync();
        var lightProduct = products[0];
        var heavyProduct = products[1];

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = lightProduct.Id,
                    Quantity = 1,
                    Weight = 1m
                },
                new ReceiveOrderProductDTO
                {
                    ProductId = heavyProduct.Id,
                    Quantity = 1,
                    Weight = 3m
                }
            ]
        });

        lightProduct = await context.Products.SingleAsync(product => product.Id == lightProduct.Id);
        heavyProduct = await context.Products.SingleAsync(product => product.Id == heavyProduct.Id);
        var lightAllocation = receipt.Products.Single(product => product.ProductId == lightProduct.Id);
        var heavyAllocation = receipt.Products.Single(product => product.ProductId == heavyProduct.Id);

        Assert.Equal(365m, receipt.WarehouseShippingCostNio);
        Assert.Equal(91.25m, lightAllocation.AllocatedWarehouseShippingCostNio);
        Assert.Equal(273.75m, heavyAllocation.AllocatedWarehouseShippingCostNio);
        Assert.Equal(456.25m, lightProduct.AllocatedShippingCostNio);
        Assert.Equal(638.75m, heavyProduct.AllocatedShippingCostNio);
        Assert.Equal(22.50m, lightProduct.UnitCostUsd);
        Assert.Equal(27.50m, heavyProduct.UnitCostUsd);
    }

    private static CreateOrderDTO CreateOrderRequest(int quantity)
    {
        return new CreateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 10m,
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
                            Quantity = quantity,
                            UnitCost = 8m,
                            SalePrice = 600m
                        }
                    ]
                }
            ]
        };
    }


    private static CreateOrderDTO CreateTwoProductOrderRequest()
    {
        return new CreateOrderDTO
        {
            SupplierId = 1,
            PurchaseCurrencyId = (int)PurchaseCurrencyOption.Usd,
            SupplierShippingCostUsd = 20m,
            ProductDetails =
            [
                new CreateOrderProductDetailDTO
                {
                    SupplierProductCode = "SOHO25120",
                    Name = "Camisa",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Color = "Azul",
                            Quantity = 1,
                            UnitCost = 10m,
                            SalePrice = 600m
                        }
                    ]
                },
                new CreateOrderProductDetailDTO
                {
                    SupplierProductCode = "SOHO25121",
                    Name = "Vestido",
                    SubcategoryId = 1,
                    Variants =
                    [
                        new CreateOrderProductVariantDTO
                        {
                            SizeId = 1,
                            Color = "Rojo",
                            Quantity = 1,
                            UnitCost = 10m,
                            SalePrice = 900m
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
        context.ShippingCompanies.Add(new ShippingCompany { Id = 1, Name = "Courier" });
        context.OrderStatuses.AddRange(
            new OrderStatus { Id = (int)OrderStatusCode.Pending, Name = "Pending" },
            new OrderStatus { Id = (int)OrderStatusCode.PartiallyReceived, Name = "PartiallyReceived" },
            new OrderStatus { Id = (int)OrderStatusCode.Received, Name = "Received" });
        context.MovementDirections.AddRange(
            new MovementDirection { Id = (int)MovementDirectionOptions.In, Name = "In" },
            new MovementDirection { Id = (int)MovementDirectionOptions.Out, Name = "Out" });
        context.InventoryMovementTypes.Add(new InventoryMovementType { Id = (int)InventoryMovementTypeOption.PurchaseReceived, Name = "PurchaseReceived" });
        context.InventoryStockBuckets.AddRange(
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.External, Name = "External" },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Available, Name = "Available" });
        context.FinancialMovementTypes.AddRange(
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SupplierPayment, Name = "SupplierPayment" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.WarehouseShippingPayment, Name = "WarehouseShippingPayment" });
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
