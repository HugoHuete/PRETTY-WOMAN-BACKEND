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
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 4));
        var product = await context.Products.SingleAsync();

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Comments = "Recepción parcial",
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
        Assert.Equal(10m, productReceipt.WarehouseShippingCostUsd);
        Assert.Equal(365m, productReceipt.WarehouseShippingCostNio);
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
        Assert.Equal(1m, receiptDetail.Weight);
        Assert.Equal(365m, receiptDetail.AllocatedWarehouseShippingCostNio);
        Assert.Equal((int)InventoryMovementTypeOption.PurchaseReceived, inventoryMovement.InventoryMovementTypeId);
        Assert.Equal((int)InventoryStockBucketOption.External, inventoryMovement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Available, inventoryMovement.ToStockBucketId);
        Assert.Equal(2, inventoryMovement.Quantity);
        Assert.Equal(orderId, inventoryMovement.OrderId);
        Assert.Null(inventoryMovement.Comments);
        Assert.Equal(365m, warehouseShippingMovement.Amount);
        Assert.Equal(productReceipt.Id, warehouseShippingMovement.ProductReceiptId);
        Assert.Equal(orderId, warehouseShippingMovement.OrderId);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsGeneralInformationForOrderReceipts()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 4));
        var product = await context.Products.SingleAsync();

        await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 1 }]
        });
        await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 5m,
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 2 }]
        });

        var receipts = await receiptService.GetAllAsync(orderId);

        Assert.Equal(2, receipts.Count);
        var firstReceipt = receipts.Single(receipt => receipt.WarehouseShippingCostUsd == 10m);
        Assert.Equal(1, firstReceipt.ProductCount);
        Assert.Equal(1, firstReceipt.TotalQuantity);
        Assert.Equal(0, firstReceipt.TrackingCount);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsReceiptProductsWeightsAndTrackings()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();
        context.OrderTrackingNumbers.Add(new OrderTrackingNumber
        {
            OrderId = orderId,
            ShippingCompanyId = 1,
            TrackingNumber = "TRACK-DETAIL"
        });
        await context.SaveChangesAsync();
        var tracking = await context.OrderTrackingNumbers.SingleAsync();

        var created = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            TrackingNumbers = [new ReceiveOrderTrackingNumberDTO { Id = tracking.Id, ShippingCostUsd = 12m, Weight = 8.5m }],
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 2, Weight = 2.5m }]
        });

        var receipt = await receiptService.GetByIdAsync(orderId, created.Id);

        Assert.Equal(created.Id, receipt.Id);
        Assert.Single(receipt.Products);
        Assert.Equal(2.5m, receipt.Products.Single().Weight);
        Assert.Equal(2, receipt.Products.Single().Quantity);
        Assert.Single(receipt.TrackingNumbers);
        Assert.Equal("TRACK-DETAIL", receipt.TrackingNumbers.Single().TrackingNumber);
        Assert.Equal(8.5m, receipt.TrackingNumbers.Single().Weight);
        Assert.Equal(12m, receipt.TrackingNumbers.Single().ShippingCost);
    }


    [Fact]
    public async Task ReceiveAsync_InterpretsUnspecifiedReceivedDateAsBusinessLocalTime()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
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
        var receiptService = CreateReceiptService(context);
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

        var receiptDetail = await context.ProductReceiptDetails.SingleAsync();
        Assert.Equal(1m, receiptDetail.Weight);
        Assert.Equal(438m, receiptDetail.AllocatedWarehouseShippingCostNio);
    }

    [Fact]
    public async Task ReceiveAsync_RejectsDirectWarehouseShippingCostWhenOrderHasTrackingNumbers()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
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
        var receiptService = CreateReceiptService(context);
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
        var receiptService = CreateReceiptService(context);
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

    [Fact]
    public async Task UpdateShippingCostAsync_RecalculatesReceiptOrderProductAndFinancialMovement()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
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

        var updated = await receiptService.UpdateShippingCostAsync(orderId, receipt.Id, new UpdateOrderReceiptDTO
        {
            WarehouseShippingCostUsd = 20m
        });

        var storedReceipt = await context.ProductReceipts.SingleAsync();
        var storedOrder = await context.Orders.SingleAsync();
        var storedProduct = await context.Products.SingleAsync();
        var financialMovement = await context.FinancialMovements
            .SingleAsync(item => item.FinancialMovementTypeId == (int)FinancialMovementTypeOption.WarehouseShippingPayment);

        Assert.Equal(20m, updated.WarehouseShippingCostUsd);
        Assert.Equal(730m, updated.WarehouseShippingCostNio);
        Assert.Equal(730m, storedReceipt.WarehouseShippingCostNio);
        Assert.Equal(20m, storedOrder.WarehouseShippingCostUsd);
        Assert.Equal(1095m, storedProduct.AllocatedShippingCostNio);
        Assert.Equal(730m, financialMovement.Amount);
        Assert.Equal(receipt.Id, financialMovement.ProductReceiptId);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_UpdatesProductWeightsAndReallocatesShipping()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateTwoProductOrderRequest());
        var products = await context.Products.OrderBy(product => product.Id).ToListAsync();
        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products =
            [
                new ReceiveOrderProductDTO { ProductId = products[0].Id, Quantity = 1, Weight = 1m },
                new ReceiveOrderProductDTO { ProductId = products[1].Id, Quantity = 1, Weight = 3m }
            ]
        });
        var details = await context.ProductReceiptDetails
            .Where(detail => detail.ProductReceiptId == receipt.Id)
            .OrderBy(detail => detail.Id)
            .ToListAsync();

        var updated = await receiptService.UpdateShippingCostAsync(orderId, receipt.Id, new UpdateOrderReceiptDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products =
            [
                new UpdateOrderReceiptProductDTO { ProductReceiptDetailId = details[0].Id, Weight = 3m },
                new UpdateOrderReceiptProductDTO { ProductReceiptDetailId = details[1].Id, Weight = 1m }
            ]
        });

        details = await context.ProductReceiptDetails.OrderBy(detail => detail.Id).ToListAsync();
        products = await context.Products.OrderBy(product => product.Id).ToListAsync();

        Assert.Equal(3m, details[0].Weight);
        Assert.Equal(1m, details[1].Weight);
        Assert.Equal(273.75m, details[0].AllocatedWarehouseShippingCostNio);
        Assert.Equal(91.25m, details[1].AllocatedWarehouseShippingCostNio);
        Assert.Equal(638.75m, products[0].AllocatedShippingCostNio);
        Assert.Equal(456.25m, products[1].AllocatedShippingCostNio);
        Assert.Equal(3m, updated.Products.Single(product => product.ProductReceiptDetailId == details[0].Id).Weight);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_RejectsIncompleteProductWeightSet()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateTwoProductOrderRequest());
        var products = await context.Products.OrderBy(product => product.Id).ToListAsync();
        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products =
            [
                new ReceiveOrderProductDTO { ProductId = products[0].Id, Quantity = 1 },
                new ReceiveOrderProductDTO { ProductId = products[1].Id, Quantity = 1 }
            ]
        });
        var detail = await context.ProductReceiptDetails.FirstAsync(detail => detail.ProductReceiptId == receipt.Id);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => receiptService.UpdateShippingCostAsync(
            orderId,
            receipt.Id,
            new UpdateOrderReceiptDTO
            {
                WarehouseShippingCostUsd = 10m,
                Products = [new UpdateOrderReceiptProductDTO { ProductReceiptDetailId = detail.Id, Weight = 2m }]
            }));

        Assert.Contains("exactamente los detalles", exception.Message);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_RejectsNonPositiveProductWeight()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 1));
        var product = await context.Products.SingleAsync();
        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 1 }]
        });
        var detail = await context.ProductReceiptDetails.SingleAsync();

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => receiptService.UpdateShippingCostAsync(
            orderId,
            receipt.Id,
            new UpdateOrderReceiptDTO
            {
                WarehouseShippingCostUsd = 10m,
                Products = [new UpdateOrderReceiptProductDTO { ProductReceiptDetailId = detail.Id, Weight = 0m }]
            }));

        Assert.Contains("mayor que cero", exception.Message);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_UpdatesIndividualTrackingCostsAndAllocation()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        context.OrderTrackingNumbers.AddRange(
            new OrderTrackingNumber { OrderId = orderId, ShippingCompanyId = 1, TrackingNumber = "TRACK-1" },
            new OrderTrackingNumber { OrderId = orderId, ShippingCompanyId = 1, TrackingNumber = "TRACK-2" });
        await context.SaveChangesAsync();
        var trackings = await context.OrderTrackingNumbers.OrderBy(tracking => tracking.Id).ToListAsync();

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            TrackingNumbers =
            [
                new ReceiveOrderTrackingNumberDTO { Id = trackings[0].Id, ShippingCostUsd = 10m },
                new ReceiveOrderTrackingNumberDTO { Id = trackings[1].Id, ShippingCostUsd = 5m }
            ],
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 2 }]
        });

        await receiptService.UpdateShippingCostAsync(orderId, receipt.Id, new UpdateOrderReceiptDTO
        {
            TrackingNumbers =
            [
                new UpdateOrderReceiptTrackingNumberDTO { Id = trackings[0].Id, ShippingCostUsd = 20m },
                new UpdateOrderReceiptTrackingNumberDTO { Id = trackings[1].Id, ShippingCostUsd = 7m }
            ]
        });

        trackings = await context.OrderTrackingNumbers.OrderBy(tracking => tracking.Id).ToListAsync();
        var storedReceipt = await context.ProductReceipts.SingleAsync();
        var detail = await context.ProductReceiptDetails.SingleAsync();
        var movement = await context.FinancialMovements
            .SingleAsync(item => item.FinancialMovementTypeId == (int)FinancialMovementTypeOption.WarehouseShippingPayment);

        Assert.Equal(20m, trackings[0].ShippingCost);
        Assert.Equal(7m, trackings[1].ShippingCost);
        Assert.Equal(27m, storedReceipt.WarehouseShippingCostUsd);
        Assert.Equal(985.50m, storedReceipt.WarehouseShippingCostNio);
        Assert.Equal(985.50m, detail.AllocatedWarehouseShippingCostNio);
        Assert.Equal(985.50m, movement.Amount);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_RemovesFinancialMovementWhenCostBecomesZero()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 2 }]
        });

        await receiptService.UpdateShippingCostAsync(orderId, receipt.Id, new UpdateOrderReceiptDTO
        {
            WarehouseShippingCostUsd = 0m
        });

        var order = await context.Orders.SingleAsync();
        product = await context.Products.SingleAsync();

        Assert.Empty(await context.FinancialMovements
            .Where(item => item.FinancialMovementTypeId == (int)FinancialMovementTypeOption.WarehouseShippingPayment)
            .ToListAsync());
        Assert.Equal(0m, order.WarehouseShippingCostUsd);
        Assert.Equal(365m, product.AllocatedShippingCostNio);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_CreatesFinancialMovementWhenCostBecomesPositive()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 0m,
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 2 }]
        });

        await receiptService.UpdateShippingCostAsync(orderId, receipt.Id, new UpdateOrderReceiptDTO
        {
            WarehouseShippingCostUsd = 10m
        });

        var movement = await context.FinancialMovements
            .SingleAsync(item => item.FinancialMovementTypeId == (int)FinancialMovementTypeOption.WarehouseShippingPayment);
        Assert.Equal(365m, movement.Amount);
        Assert.Equal(receipt.Id, movement.ProductReceiptId);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_RejectsTrackingThatDoesNotBelongToReceipt()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        context.OrderTrackingNumbers.AddRange(
            new OrderTrackingNumber { OrderId = orderId, ShippingCompanyId = 1, TrackingNumber = "TRACK-1" },
            new OrderTrackingNumber { OrderId = orderId, ShippingCompanyId = 1, TrackingNumber = "TRACK-2" });
        await context.SaveChangesAsync();
        var tracking = await context.OrderTrackingNumbers.OrderBy(item => item.Id).FirstAsync();
        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            TrackingNumbers = [new ReceiveOrderTrackingNumberDTO { Id = tracking.Id, ShippingCostUsd = 10m }],
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 2 }]
        });
        var otherTracking = await context.OrderTrackingNumbers.OrderBy(item => item.Id).LastAsync();

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => receiptService.UpdateShippingCostAsync(
            orderId,
            receipt.Id,
            new UpdateOrderReceiptDTO
            {
                TrackingNumbers = [new UpdateOrderReceiptTrackingNumberDTO { Id = otherTracking.Id, ShippingCostUsd = 20m }]
            }));

        Assert.Contains("exactamente los trackings", exception.Message);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_RejectsNegativeShippingCost()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();
        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 2 }]
        });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => receiptService.UpdateShippingCostAsync(
            orderId,
            receipt.Id,
            new UpdateOrderReceiptDTO { WarehouseShippingCostUsd = -1m }));

        Assert.Contains("no puede ser negativo", exception.Message);
    }

    [Fact]
    public async Task UpdateShippingCostAsync_PropagatesCostToEverySaleLineWithoutChangingSaleAmount()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Products = [new ReceiveOrderProductDTO { ProductId = product.Id, Quantity = 2 }]
        });
        var saleProduct = new SaleProduct
        {
            SaleId = 1,
            ProductId = product.Id,
            Quantity = 1,
            UnitCostAtSale = product.UnitCostNio,
            OriginalUnitPrice = 600m,
            FinalUnitPrice = 600m,
            LineTotal = 600m,
            TotalCostAtSale = product.UnitCostNio,
            GrossProfit = 600m - product.UnitCostNio
        };
        context.SaleProducts.Add(saleProduct);
        await context.SaveChangesAsync();

        await receiptService.UpdateShippingCostAsync(orderId, receipt.Id, new UpdateOrderReceiptDTO
        {
            WarehouseShippingCostUsd = 20m
        });

        var storedSaleProduct = await context.SaleProducts.SingleAsync();
        var storedProduct = await context.Products.SingleAsync();

        Assert.Equal(storedProduct.UnitCostNio, storedSaleProduct.UnitCostAtSale);
        Assert.Equal(storedProduct.UnitCostNio, storedSaleProduct.TotalCostAtSale);
        Assert.Equal(600m, storedSaleProduct.LineTotal);
        Assert.Equal(600m - storedProduct.UnitCostNio, storedSaleProduct.GrossProfit);
    }

    [Fact]
    public async Task ReceiveAsync_AllowsExplicitSurplusAndKeepsPurchaseReceivedMovement()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        var receipt = await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 10m,
            Comments = "Vino una unidad adicional no solicitada.",
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 3,
                    IsSurplus = true
                }
            ]
        });

        var order = await context.Orders.SingleAsync(order => order.Id == orderId);
        product = await context.Products.SingleAsync(storedProduct => storedProduct.Id == product.Id);
        var inventoryMovement = await context.InventoryMovements.SingleAsync();
        var receivedProduct = Assert.Single(receipt.Products);

        Assert.Equal((int)OrderStatusCode.Received, order.OrderStatusId);
        Assert.Equal(order.MerchandiseTotalNio, order.ReceivedAmountNio);
        Assert.Equal(3, product.ReceivedQuantity);
        Assert.Equal(3, product.AvailableQuantity);
        Assert.True(receivedProduct.IsSurplus);
        Assert.Equal((int)InventoryMovementTypeOption.PurchaseReceived, inventoryMovement.InventoryMovementTypeId);
        Assert.Equal(3, inventoryMovement.Quantity);
        Assert.Null(inventoryMovement.Comments);
        Assert.Equal(12m, product.UnitCostUsd);
    }

    [Fact]
    public async Task ReceiveAsync_AllowsSurplusOnlyReceiptAfterOrderWasFullyReceived()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 1));
        var product = await context.Products.SingleAsync();

        await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 0,
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1
                }
            ]
        });

        await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 0,
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    IsSurplus = true
                }
            ]
        });

        product = await context.Products.SingleAsync(storedProduct => storedProduct.Id == product.Id);
        var movements = await context.InventoryMovements.OrderBy(movement => movement.Id).ToListAsync();

        Assert.Equal(2, product.ReceivedQuantity);
        Assert.Equal(2, product.AvailableQuantity);
        Assert.All(movements, movement => Assert.Equal((int)InventoryMovementTypeOption.PurchaseReceived, movement.InventoryMovementTypeId));
    }

    [Fact]
    public async Task ReceiveAsync_RejectsQuantityAbovePendingWhenLineIsNotMarkedAsSurplus()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 0,
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 3
                }
            ]
        }));

        Assert.Contains("supera la cantidad pendiente", exception.Message);
    }

    [Fact]
    public async Task ReceiveAsync_AllowsSurplusWithoutLineComment()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var orderService = new OrderService(context, Mapper);
        var receiptService = CreateReceiptService(context);
        var orderId = await orderService.CreateAsync(CreateOrderRequest(quantity: 2));
        var product = await context.Products.SingleAsync();

        await receiptService.ReceiveAsync(orderId, new ReceiveOrderDTO
        {
            WarehouseShippingCostUsd = 0,
            Comments = "Sobrante registrado al recibir la compra.",
            Products =
            [
                new ReceiveOrderProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 3,
                    IsSurplus = true
                }
            ]
        });

        var inventoryMovement = await context.InventoryMovements.SingleAsync();
        Assert.Null(inventoryMovement.Comments);
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
                            Variant = "Azul",
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
                            Variant = "Azul",
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
                            Variant = "Rojo",
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
            new OrderStatus { Id = (int)OrderStatusCode.Received, Name = "Received" },
            new OrderStatus { Id = (int)OrderStatusCode.PendingRefund, Name = "PendingRefund" });
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

    private static OrderReceiptService CreateReceiptService(ApplicationDbContext context)
    {
        return new OrderReceiptService(context, new InventoryService(context));
    }
}
