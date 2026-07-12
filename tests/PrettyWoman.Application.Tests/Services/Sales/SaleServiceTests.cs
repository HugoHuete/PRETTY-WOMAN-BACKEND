using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Sales;

public class SaleServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesInStoreSaleWithMultipleFullPayments()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 3, salePrice: 1000m, unitCostNio: 400m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleDate = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc),
            SaleChannelId = (int)SaleChannelOption.InStoreSale,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO
                {
                    MovementDate = new DateTime(2026, 7, 10, 10, 1, 0, DateTimeKind.Utc),
                    PaymentMethodId = 1,
                    ProductAmount = 500m
                },
                new CreateSalePaymentMovementDTO
                {
                    MovementDate = new DateTime(2026, 7, 10, 10, 2, 0, DateTimeKind.Utc),
                    PaymentMethodId = (int)PaymentMethodOption.Card,
                    PaymentTerminalId = 1,
                    ProductAmount = 500m
                }
            ]
        });

        var sale = await context.Sales
            .Include(item => item.Products)
            .Include(item => item.PaymentMovements)
            .SingleAsync(item => item.Id == saleId);
        var updatedProduct = await context.Products.SingleAsync(item => item.Id == product.Id);
        var movements = await context.FinancialMovements.OrderBy(item => item.Id).ToListAsync();

        Assert.Equal((int)SalePaymentStatusOption.Paid, sale.SalePaymentStatusId);
        Assert.Equal(1000m, sale.Subtotal);
        Assert.Equal(1000m, sale.Total);
        Assert.Equal(2, updatedProduct.AvailableQuantity);
        Assert.Equal(2, sale.PaymentMovements.Count);
        Assert.Equal(2, movements.Count);
        Assert.Equal(500m, movements[0].Amount);
        Assert.Equal(470.25m, movements[1].Amount);
    }

    [Fact]
    public async Task CreateAsync_RejectsInStoreSaleWithoutFullPayment()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 3, salePrice: 1000m, unitCostNio: 400m);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.InStoreSale,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO
                {
                    PaymentMethodId = 1,
                    ProductAmount = 500m
                }
            ]
        }));

        Assert.Contains("ventas en local", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await context.Sales.AnyAsync());
    }

    [Fact]
    public async Task PatchHeaderAsync_UpdatesHeaderWithoutReplacingProductsOrInventoryMovements()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 3, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ]
        });

        var originalSaleProductId = await context.SaleProducts
            .Where(item => item.SaleId == saleId)
            .Select(item => item.Id)
            .SingleAsync();
        var originalInventoryMovementId = await context.InventoryMovements
            .Where(item => item.SaleProductId == originalSaleProductId)
            .Select(item => item.Id)
            .SingleAsync();

        await service.PatchHeaderAsync(saleId, new PatchSaleHeaderDTO
        {
            SaleDate = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc),
            Comments = "Cliente pidio envolver para regalo"
        });

        var sale = await context.Sales.Include(item => item.Products).SingleAsync(item => item.Id == saleId);
        var updatedProduct = await context.Products.SingleAsync(item => item.Id == product.Id);
        var inventoryMovement = await context.InventoryMovements.SingleAsync(item => item.Id == originalInventoryMovementId);

        Assert.Equal(new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc), sale.SaleDate);
        Assert.Equal("Cliente pidio envolver para regalo", sale.Comments);
        Assert.Equal(originalSaleProductId, sale.Products.Single().Id);
        Assert.Equal(originalSaleProductId, inventoryMovement.SaleProductId);
        Assert.Equal(new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc), inventoryMovement.MovementDate);
        Assert.Equal(2, updatedProduct.AvailableQuantity);
    }

    [Fact]
    public async Task PatchHeaderAsync_AllowsClearingNullableHeaderFields()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 3, salePrice: 500m, unitCostNio: 200m);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        context.Clients.Add(new Client { Id = 1, Name = "Cliente prueba" });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Pending,
            ClientId = 1,
            Comments = "Entregar por la tarde",
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ]
        });

        var patch = JsonSerializer.Deserialize<PatchSaleHeaderDTO>(
            """
            {
              "ClientId": null,
              "MunicipalityId": null,
              "Comments": null
            }
            """)!;

        await service.PatchHeaderAsync(saleId, patch);

        var sale = await context.Sales.SingleAsync(item => item.Id == saleId);

        Assert.Null(sale.ClientId);
        Assert.Null(sale.Comments);
    }

    [Fact]
    public async Task ReplaceProductsAsync_ReplacesProductsAndRecalculatesInventoryAndTotals()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var firstProduct = await AddProductAsync(context, availableQuantity: 3, salePrice: 500m, unitCostNio: 200m);
        var secondProduct = await AddProductAsync(context, availableQuantity: 4, salePrice: 300m, unitCostNio: 100m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = firstProduct.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ]
        });

        await service.ReplaceProductsAsync(saleId, new ReplaceSaleProductsDTO
        {
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = secondProduct.Id,
                    Quantity = 2,
                    DiscountSourceId = (int)DiscountSourceOption.None,
                    DiscountAmount = 25m
                }
            ]
        });

        var sale = await context.Sales.Include(item => item.Products).SingleAsync(item => item.Id == saleId);
        var reloadedFirstProduct = await context.Products.SingleAsync(item => item.Id == firstProduct.Id);
        var reloadedSecondProduct = await context.Products.SingleAsync(item => item.Id == secondProduct.Id);
        var movements = await context.InventoryMovements.ToListAsync();

        Assert.Equal(600m, sale.Subtotal);
        Assert.Equal(50m, sale.TotalDiscount);
        Assert.Equal(550m, sale.Total);
        Assert.Equal(firstProduct.Quantity, reloadedFirstProduct.AvailableQuantity);
        Assert.Equal(2, reloadedSecondProduct.AvailableQuantity);
        Assert.Single(sale.Products);
        Assert.Equal(secondProduct.Id, sale.Products.Single().ProductId);
        Assert.Single(movements);
        Assert.Equal(sale.Products.Single().Id, movements.Single().SaleProductId);
    }
    [Fact]
    public async Task ReplaceProductsAsync_AllowsPaidSaleWithPendingDeliveryAndMarksRefundPendingWhenOverpaid()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var firstProduct = await AddProductAsync(context, availableQuantity: 3, salePrice: 500m, unitCostNio: 200m);
        var secondProduct = await AddProductAsync(context, availableQuantity: 4, salePrice: 300m, unitCostNio: 100m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.ReadyForDelivery,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = firstProduct.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO
                {
                    PaymentMethodId = 1,
                    ProductAmount = 500m
                }
            ]
        });

        context.SaleDeliveries.Add(new SaleDelivery
        {
            Code = "DEL-001",
            SaleId = saleId,
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            DeliveryStatusId = (int)DeliveryStatusCode.Pending,
            AmountToCollect = 0,
            ShippingChargedToClient = 0,
            ShippingPaidToAgency = 0,
            UserId = "test-user"
        });
        await context.SaveChangesAsync();

        await service.ReplaceProductsAsync(saleId, new ReplaceSaleProductsDTO
        {
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = secondProduct.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ]
        });

        var sale = await context.Sales.Include(item => item.Products).SingleAsync(item => item.Id == saleId);
        var reloadedFirstProduct = await context.Products.SingleAsync(item => item.Id == firstProduct.Id);
        var reloadedSecondProduct = await context.Products.SingleAsync(item => item.Id == secondProduct.Id);

        Assert.Equal(300m, sale.Total);
        Assert.Equal((int)SalePaymentStatusOption.RefundPending, sale.SalePaymentStatusId);
        Assert.Equal(firstProduct.Quantity, reloadedFirstProduct.AvailableQuantity);
        Assert.Equal(3, reloadedSecondProduct.AvailableQuantity);
        Assert.Single(sale.Products);
        Assert.Equal(secondProduct.Id, sale.Products.Single().ProductId);
    }

    [Fact]
    public async Task AddPaymentMovementAsync_AddsPaymentAndFinancialMovement()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 3, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ]
        });

        var paymentId = await service.AddPaymentMovementAsync(saleId, new CreateSalePaymentMovementDTO
        {
            MovementDate = new DateTime(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc),
            PaymentMethodId = (int)PaymentMethodOption.Cash,
            ProductAmount = 200m
        });

        var sale = await context.Sales.Include(item => item.PaymentMovements).SingleAsync(item => item.Id == saleId);
        var movement = await context.FinancialMovements.SingleAsync(item => item.SalePaymentMovementId == paymentId);

        Assert.Equal((int)SalePaymentStatusOption.PartiallyPaid, sale.SalePaymentStatusId);
        Assert.Single(sale.PaymentMovements);
        Assert.Equal(200m, movement.Amount);
        Assert.Equal((int)MovementDirectionOptions.In, movement.MovementDirectionId);
        Assert.Equal((int)FinancialMovementTypeOption.SalePayment, movement.FinancialMovementTypeId);
    }

    [Fact]
    public async Task UpdatePaymentMovementAsync_UpdatesPaymentAndFinancialMovement()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 3, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO
                {
                    PaymentMethodId = (int)PaymentMethodOption.Cash,
                    ProductAmount = 200m
                }
            ]
        });
        var paymentId = await context.SalePaymentMovements
            .Where(item => item.SaleId == saleId)
            .Select(item => item.Id)
            .SingleAsync();

        await service.UpdatePaymentMovementAsync(saleId, paymentId, new UpdateSalePaymentMovementDTO
        {
            MovementDate = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
            PaymentMethodId = (int)PaymentMethodOption.Transfer,
            ProductAmount = 500m
        });

        var sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        var payment = await context.SalePaymentMovements.SingleAsync(item => item.Id == paymentId);
        var movement = await context.FinancialMovements.SingleAsync(item => item.SalePaymentMovementId == paymentId);

        Assert.Equal((int)SalePaymentStatusOption.Paid, sale.SalePaymentStatusId);
        Assert.Equal((int)PaymentMethodOption.Transfer, payment.PaymentMethodId);
        Assert.Equal(500m, payment.GrossAmount);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc), movement.MovementDate);
        Assert.Equal(500m, movement.Amount);
    }

    [Fact]
    public async Task RefundPaymentMovementAsync_AllowsPartialCashRefundAndUpdatesPaymentStatus()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 3, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO
                {
                    PaymentMethodId = (int)PaymentMethodOption.Cash,
                    ProductAmount = 500m
                }
            ]
        });
        var paymentId = await context.SalePaymentMovements
            .Where(item => item.SaleId == saleId)
            .Select(item => item.Id)
            .SingleAsync();

        var refundId = await service.RefundPaymentMovementAsync(saleId, paymentId, new RefundSalePaymentMovementDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Transfer,
            ProductAmount = 200m
        });

        var sale = await context.Sales.Include(item => item.PaymentMovements).SingleAsync(item => item.Id == saleId);
        var refund = await context.SalePaymentMovements.SingleAsync(item => item.Id == refundId);
        var movement = await context.FinancialMovements.SingleAsync(item => item.SalePaymentMovementId == refundId);

        Assert.Equal((int)SalePaymentStatusOption.PartiallyPaid, sale.SalePaymentStatusId);
        Assert.Equal((int)MovementDirectionOptions.Out, refund.MovementDirectionId);
        Assert.Equal(paymentId, refund.ReversedSalePaymentMovementId);
        Assert.Equal(200m, refund.GrossAmount);
        Assert.Equal((int)FinancialMovementTypeOption.CustomerRefund, movement.FinancialMovementTypeId);
        Assert.Equal((int)MovementDirectionOptions.Out, movement.MovementDirectionId);
    }

    [Fact]
    public async Task RefundPaymentMovementAsync_RequiresFullRefundForCardPayments()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 3, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO
                {
                    PaymentMethodId = (int)PaymentMethodOption.Card,
                    PaymentTerminalId = 1,
                    ProductAmount = 500m
                }
            ]
        });
        var paymentId = await context.SalePaymentMovements
            .Where(item => item.SaleId == saleId)
            .Select(item => item.Id)
            .SingleAsync();

        await Assert.ThrowsAsync<AppBadRequestException>(() => service.RefundPaymentMovementAsync(saleId, paymentId, new RefundSalePaymentMovementDTO
        {
            ProductAmount = 200m
        }));

        var refundId = await service.RefundPaymentMovementAsync(saleId, paymentId, new RefundSalePaymentMovementDTO());
        var sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        var originalPayment = await context.SalePaymentMovements.SingleAsync(item => item.Id == paymentId);
        var refund = await context.SalePaymentMovements.SingleAsync(item => item.Id == refundId);
        var movement = await context.FinancialMovements.SingleAsync(item => item.SalePaymentMovementId == refundId);

        Assert.Equal((int)SalePaymentStatusOption.Unpaid, sale.SalePaymentStatusId);
        Assert.Equal(originalPayment.GrossAmount, refund.GrossAmount);
        Assert.Equal(originalPayment.CommissionAmount, refund.CommissionAmount);
        Assert.Equal(originalPayment.IncomeTaxAmount, refund.IncomeTaxAmount);
        Assert.Equal(originalPayment.NetReceivedAmount, movement.Amount);
        Assert.Equal((int)FinancialMovementTypeOption.CustomerRefund, movement.FinancialMovementTypeId);
    }
    [Fact]
    public async Task UpdatePaymentMovementAsync_PatchesOnlyTheSpecifiedFields()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products =
            [
                new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO
                {
                    MovementDate = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc),
                    PaymentMethodId = (int)PaymentMethodOption.Cash,
                    ProductAmount = 200m
                }
            ]
        });
        var paymentId = await context.SalePaymentMovements.Where(item => item.SaleId == saleId).Select(item => item.Id).SingleAsync();

        await service.UpdatePaymentMovementAsync(saleId, paymentId, new UpdateSalePaymentMovementDTO { ProductAmount = 250m });

        var payment = await context.SalePaymentMovements.SingleAsync(item => item.Id == paymentId);
        Assert.Equal((int)PaymentMethodOption.Cash, payment.PaymentMethodId);
        Assert.Equal(new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc), payment.MovementDate);
        Assert.Equal(250m, payment.GrossAmount);
    }

    [Fact]
    public async Task CreateAsync_RequiresATerminalForCardPayments()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products =
            [
                new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO { PaymentMethodId = (int)PaymentMethodOption.Card, ProductAmount = 500m }
            ]
        }));
    }

    [Fact]
    public async Task AdjustPaymentMovementsAsync_ReplacesAnInStorePaymentAtomically()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.InStoreSale,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products =
            [
                new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }
            ],
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO { PaymentMethodId = (int)PaymentMethodOption.Cash, ProductAmount = 500m }
            ]
        });
        var cashPaymentId = await context.SalePaymentMovements.Where(item => item.SaleId == saleId).Select(item => item.Id).SingleAsync();

        await service.AdjustPaymentMovementsAsync(saleId, new AdjustSalePaymentMovementsDTO
        {
            PaymentMovements =
            [
                new CreateSalePaymentMovementDTO { PaymentMethodId = (int)PaymentMethodOption.Card, PaymentTerminalId = 1, ProductAmount = 500m }
            ],
            Refunds =
            [
                new CreateSalePaymentRefundDTO { PaymentMovementId = cashPaymentId, ProductAmount = 500m }
            ]
        });

        var sale = await context.Sales.Include(item => item.PaymentMovements).SingleAsync(item => item.Id == saleId);
        Assert.Equal((int)SalePaymentStatusOption.Paid, sale.SalePaymentStatusId);
        Assert.Equal(3, sale.PaymentMovements.Count);
        Assert.Contains(sale.PaymentMovements, item => item.MovementDirectionId == (int)MovementDirectionOptions.Out && item.ReversedSalePaymentMovementId == cashPaymentId);
        Assert.Contains(sale.PaymentMovements, item => item.MovementDirectionId == (int)MovementDirectionOptions.In && item.PaymentMethodId == (int)PaymentMethodOption.Card);
    }
    [Fact]
    public async Task CreateDeliveryAsync_CreatesCodDeliveryMarksSaleReadyAndCanBeSent()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });

        var deliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-001",
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            ShippingChargedToClient = 50m,
            DeliveryAddress = "De la rotonda 2 cuadras al norte"
        });

        var delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        var sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        Assert.Equal(550m, delivery.AmountToCollect);
        Assert.Equal("De la rotonda 2 cuadras al norte", delivery.DeliveryAddress);
        Assert.Equal((int)SaleStatusOption.ReadyForDelivery, sale.SaleStatusId);

        await service.MarkDeliveryAsSentAsync(saleId, deliveryId);

        delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        Assert.Equal((int)DeliveryStatusCode.Sent, delivery.DeliveryStatusId);
        Assert.Equal((int)SaleStatusOption.SentForDelivery, sale.SaleStatusId);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-004",
            MunicipalityId = 1,
            DeliveryAgencyId = 1
        }));
        Assert.Contains("envio activo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task DeliveryTransitions_CancelFailAndCompleteFollowTheirExpectedFlow()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });

        var pendingDeliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-CANCELLED",
            MunicipalityId = 1,
            DeliveryAgencyId = 1
        });
        await Assert.ThrowsAsync<AppBadRequestException>(() => service.MarkDeliveryAsCompletedAsync(saleId, pendingDeliveryId));
        await service.CancelDeliveryAsync(saleId, pendingDeliveryId);

        var failedDeliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-FAILED",
            MunicipalityId = 1,
            DeliveryAgencyId = 1
        });
        await service.MarkDeliveryAsSentAsync(saleId, failedDeliveryId);
        await service.MarkDeliveryAsFailedAsync(saleId, failedDeliveryId);

        var failedDelivery = await context.SaleDeliveries.SingleAsync(item => item.Id == failedDeliveryId);
        var sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        Assert.Equal((int)DeliveryStatusCode.Failed, failedDelivery.DeliveryStatusId);
        Assert.Equal((int)SaleStatusOption.ReadyForDelivery, sale.SaleStatusId);

        var completedDeliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-COMPLETED",
            MunicipalityId = 1,
            DeliveryAgencyId = 1
        });
        await service.MarkDeliveryAsSentAsync(saleId, completedDeliveryId);
        await service.MarkDeliveryAsCompletedAsync(saleId, completedDeliveryId);

        var completedDelivery = await context.SaleDeliveries.SingleAsync(item => item.Id == completedDeliveryId);
        sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        Assert.Equal((int)DeliveryStatusCode.Completed, completedDelivery.DeliveryStatusId);
        Assert.Equal((int)SaleStatusOption.Completed, sale.SaleStatusId);
    }

    [Fact]
    public async Task CancelAsync_CancelsSaleAndPendingDeliveryAndRestoresInventory()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 2, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);

        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });
        var deliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-SALE-CANCELLED",
            MunicipalityId = 1,
            DeliveryAgencyId = 1
        });

        await service.CancelAsync(saleId);

        var sale = await context.Sales.Include(item => item.Products).SingleAsync(item => item.Id == saleId);
        var delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        var updatedProduct = await context.Products.SingleAsync(item => item.Id == product.Id);
        var inventoryMovement = await context.InventoryMovements.SingleAsync(item =>
            item.SaleProductId == sale.Products.Single().Id &&
            item.InventoryMovementTypeId == (int)InventoryMovementTypeOption.SaleCancelled);

        Assert.Equal((int)SaleStatusOption.Cancelled, sale.SaleStatusId);
        Assert.Equal((int)SaleProductStatusOption.Cancelled, sale.Products.Single().SaleProductStatusId);
        Assert.Equal((int)DeliveryStatusCode.Cancelled, delivery.DeliveryStatusId);
        Assert.Equal(2, updatedProduct.AvailableQuantity);
        Assert.Equal((int)InventoryStockBucketOption.OutOfInventory, inventoryMovement.FromStockBucketId);
        Assert.Equal((int)InventoryStockBucketOption.Available, inventoryMovement.ToStockBucketId);
    }

    [Fact]
    public async Task CancelAsync_RejectsSaleWithPaymentsPendingRefund()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.Reserved,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }],
            PaymentMovements = [new CreateSalePaymentMovementDTO { PaymentMethodId = (int)PaymentMethodOption.Cash, ProductAmount = 500m }]
        });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CancelAsync(saleId));

        Assert.Contains("reembolso", exception.Message, StringComparison.OrdinalIgnoreCase);
        var sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        Assert.Equal((int)SaleStatusOption.Reserved, sale.SaleStatusId);
    }

    [Fact]
    public async Task CreateDeliveryAsync_RequiresFullPaymentWhenAgencyCannotCollectCash()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        context.DeliveryAgencies.Add(new DeliveryAgency { Id = 2, Name = "Agencia prepago", PhoneNumber = "88881111", CanCollectCashOnDelivery = false });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-002",
            MunicipalityId = 1,
            DeliveryAgencyId = 2
        }));

        Assert.Contains("pagada completamente", exception.Message, StringComparison.OrdinalIgnoreCase);

        await service.AddPaymentMovementAsync(saleId, new CreateSalePaymentMovementDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Cash,
            ProductAmount = 500m
        });
        var deliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-PAID-002",
            MunicipalityId = 1,
            DeliveryAgencyId = 2,
            ShippingChargedToClient = 50m
        });

        var delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        Assert.Equal(0m, delivery.AmountToCollect);
        Assert.Equal(50m, delivery.ShippingChargedToClient);
    }

    [Fact]
    public async Task AddPaymentMovementAsync_UpdatesAmountToCollectForActiveDelivery()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });
        await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-003",
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            ShippingChargedToClient = 50m,
            DeliveryAddress = "De la rotonda 2 cuadras al norte"
        });

        await service.AddPaymentMovementAsync(saleId, new CreateSalePaymentMovementDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Cash,
            ProductAmount = 200m
        });

        var delivery = await context.SaleDeliveries.SingleAsync(item => item.SaleId == saleId);
        Assert.Equal(350m, delivery.AmountToCollect);
    }
    [Fact]
    public async Task UpdateDeliveryAsync_UpdatesAllowedFieldsAndRecalculatesAmountToCollect()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.AddRange(
            new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 },
            new Municipality { Id = 2, Name = "Tipitapa", DepartmentId = 1 });
        context.Clients.Add(new Client { Id = 1, Name = "Cliente original" });
        context.Clients.Add(new Client { Id = 2, Name = "Cliente actualizado" });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });
        var deliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-005",
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            ClientId = 1,
            ShippingChargedToClient = 50m,
            DeliveryAddress = "Direccion original"
        });

        await service.UpdateDeliveryAsync(saleId, deliveryId, new PatchSaleDeliveryDTO
        {
            Code = " DEL-005-UPDATED ",
            MunicipalityId = 2,
            ClientId = 2,
            DeliveryAddress = " Nueva direccion ",
            ShippingChargedToClient = 75m
        });

        var delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        Assert.Equal("DEL-005-UPDATED", delivery.Code);
        Assert.Equal(2, delivery.MunicipalityId);
        Assert.Equal(2, delivery.ClientId);
        Assert.Equal("Nueva direccion", delivery.DeliveryAddress);
        Assert.Equal(75m, delivery.ShippingChargedToClient);
        Assert.Equal(575m, delivery.AmountToCollect);
    }

    [Fact]
    public async Task UpdateDeliveryAsync_RejectsAgencyWithoutCashCollectionWhenSaleIsNotFullyPaid()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        context.DeliveryAgencies.Add(new DeliveryAgency { Id = 2, Name = "Agencia prepago", PhoneNumber = "88881111", CanCollectCashOnDelivery = false });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });
        var deliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-006",
            MunicipalityId = 1,
            DeliveryAgencyId = 1
        });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.UpdateDeliveryAsync(saleId, deliveryId, new PatchSaleDeliveryDTO
        {
            DeliveryAgencyId = 2
        }));

        Assert.Contains("pagada completamente", exception.Message, StringComparison.OrdinalIgnoreCase);
        var delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        Assert.Equal(1, delivery.DeliveryAgencyId);
    }
    [Fact]
    public async Task AddPaymentMovementAsync_AppliesPaymentToProductsAndShippingAndClearsAmountToCollect()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });
        var deliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-ALLOC-001",
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            ShippingChargedToClient = 90m
        });

        var paymentId = await service.AddPaymentMovementAsync(saleId, new CreateSalePaymentMovementDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Transfer,
            ProductAmount = 500m,
            ShippingAmount = 90m,
            SaleDeliveryId = deliveryId
        });

        var payment = await context.SalePaymentMovements.SingleAsync(item => item.Id == paymentId);
        var sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        var delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        var financialMovement = await context.FinancialMovements.SingleAsync(item => item.SalePaymentMovementId == paymentId);

        Assert.Equal(590m, payment.GrossAmount);
        Assert.Equal(500m, payment.ProductAmount);
        Assert.Equal(90m, payment.ShippingAmount);
        Assert.Equal(deliveryId, payment.SaleDeliveryId);
        Assert.Equal((int)SalePaymentStatusOption.Paid, sale.SalePaymentStatusId);
        Assert.Equal(0m, delivery.AmountToCollect);
        Assert.Equal(590m, financialMovement.Amount);
    }

    [Fact]
    public async Task AddPaymentMovementAsync_RejectsShippingPaymentGreaterThanShippingCharge()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });
        var deliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-ALLOC-002",
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            ShippingChargedToClient = 90m
        });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.AddPaymentMovementAsync(saleId, new CreateSalePaymentMovementDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Transfer,
            ProductAmount = 500m,
            ShippingAmount = 100m,
            SaleDeliveryId = deliveryId
        }));

        Assert.Contains("envio", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task MarkDeliveryAsSentAsync_RequiresShippingPaymentForAgencyWithoutCashCollection()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        context.DeliveryAgencies.Add(new DeliveryAgency { Id = 2, Name = "Agencia prepago", PhoneNumber = "88881111", CanCollectCashOnDelivery = false });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var service = CreateService(context);
        var saleId = await service.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }],
            PaymentMovements = [new CreateSalePaymentMovementDTO { PaymentMethodId = (int)PaymentMethodOption.Transfer, ProductAmount = 500m }]
        });
        var deliveryId = await service.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-NONCOD-SEND",
            MunicipalityId = 1,
            DeliveryAgencyId = 2,
            ShippingChargedToClient = 90m
        });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.MarkDeliveryAsSentAsync(saleId, deliveryId));
        Assert.Contains("envio deben estar pagados", exception.Message, StringComparison.OrdinalIgnoreCase);

        await service.AddPaymentMovementAsync(saleId, new CreateSalePaymentMovementDTO
        {
            PaymentMethodId = (int)PaymentMethodOption.Transfer,
            ProductAmount = 0,
            ShippingAmount = 90m,
            SaleDeliveryId = deliveryId
        });

        await service.MarkDeliveryAsSentAsync(saleId, deliveryId);
        var sale = await context.Sales.SingleAsync(item => item.Id == saleId);
        Assert.Equal((int)SaleStatusOption.SentForDelivery, sale.SaleStatusId);
    }

    [Fact]
    public async Task GetPendingDeliveriesAsync_ReturnsOnlyUnreconciledCompletedOrFailedDeliveriesAndFiltersByAgency()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.DeliveryAgencies.Add(new DeliveryAgency { Id = 2, Name = "Agencia dos", PhoneNumber = "88882222", CanCollectCashOnDelivery = true });
        context.Clients.Add(new Client { Id = 1, Name = "Ana Perez" });
        context.Sales.Add(new Sale
        {
            SaleDate = new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc),
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            SaleStatusId = (int)SaleStatusOption.ReadyForDelivery,
            Total = 500m,
            UserId = "test-user"
        });
        await context.SaveChangesAsync();
        var saleId = await context.Sales.Select(item => item.Id).SingleAsync();
        context.SaleDeliveries.AddRange(
            new SaleDelivery
            {
                Code = "DEL-PENDING-COMPLETE", SaleId = saleId, ClientId = 1, DeliveryAgencyId = 1,
                DeliveryStatusId = (int)DeliveryStatusCode.Completed, MunicipalityId = 1, AmountToCollect = 550m,
                ShippingChargedToClient = 50m, DeliveryAddress = "Managua", UserId = "test-user"
            },
            new SaleDelivery
            {
                Code = "DEL-PENDING-FAILED", SaleId = saleId, DeliveryAgencyId = 1,
                DeliveryStatusId = (int)DeliveryStatusCode.Failed, MunicipalityId = 1, UserId = "test-user"
            },
            new SaleDelivery
            {
                Code = "DEL-NOT-FINAL", SaleId = saleId, DeliveryAgencyId = 1,
                DeliveryStatusId = (int)DeliveryStatusCode.Pending, MunicipalityId = 1, UserId = "test-user"
            },
            new SaleDelivery
            {
                Code = "DEL-OTHER-AGENCY", SaleId = saleId, DeliveryAgencyId = 2,
                DeliveryStatusId = (int)DeliveryStatusCode.Completed, MunicipalityId = 1, UserId = "test-user"
            },
            new SaleDelivery
            {
                Code = "DEL-RECONCILED", SaleId = saleId, DeliveryAgencyId = 1,
                DeliveryStatusId = (int)DeliveryStatusCode.Completed, MunicipalityId = 1,
                DeliveryAgencyReconciliationId = 1, UserId = "test-user"
            });
        await context.SaveChangesAsync();
        var service = new DeliveryAgencyReconciliationService(context, new TestCurrentUserService());

        var allPending = (await service.GetPendingDeliveriesAsync()).ToList();
        var agencyOnePending = (await service.GetPendingDeliveriesAsync(1)).ToList();

        Assert.Equal(3, allPending.Count);
        Assert.Equal(2, agencyOnePending.Count);
        Assert.All(agencyOnePending, item => Assert.Equal(1, item.DeliveryAgencyId));
        var completedDelivery = Assert.Single(agencyOnePending, item => item.Code == "DEL-PENDING-COMPLETE");
        Assert.Equal("Agencia COD", completedDelivery.DeliveryAgencyName);
        Assert.Equal("Ana Perez", completedDelivery.ClientName);
        Assert.Equal(500m, completedDelivery.SaleTotal);
        Assert.Equal(550m, completedDelivery.AmountToCollect);
    }

    [Fact]
    public async Task CreateAgencyReconciliationAsync_RecordsUsdCollectionChangeAndSettlementCashFlows()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        await context.SaveChangesAsync();
        var product = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var saleService = CreateService(context);
        var reconciliationService = new DeliveryAgencyReconciliationService(context, new TestCurrentUserService());
        var saleId = await saleService.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = product.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });
        var deliveryId = await saleService.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-RECON-USD",
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            ShippingChargedToClient = 50m
        });
        await saleService.MarkDeliveryAsSentAsync(saleId, deliveryId);
        await saleService.MarkDeliveryAsCompletedAsync(saleId, deliveryId);

        var reconciliationId = await reconciliationService.CreateAsync(new CreateDeliveryAgencyReconciliationDTO
        {
            DeliveryAgencyId = 1,
            SettlementExchangeRate = 36m,
            AmountReceivedUsd = 20m,
            AmountPaidToAgencyNio = 250m,
            Deliveries =
            [
                new ReconcileSaleDeliveryDTO
                {
                    SaleDeliveryId = deliveryId,
                    AmountCollectedUsd = 20m,
                    CollectionExchangeRate = 36m,
                    ChangeGivenNio = 170m,
                    ShippingPaidToAgency = 80m
                }
            ]
        });

        var delivery = await context.SaleDeliveries.SingleAsync(item => item.Id == deliveryId);
        var sale = await context.Sales.Include(item => item.PaymentMovements).SingleAsync(item => item.Id == saleId);
        var financialMovements = await context.FinancialMovements
            .Where(item => item.DeliveryAgencyReconciliationId == reconciliationId)
            .OrderBy(item => item.MovementDirectionId)
            .ToListAsync();

        Assert.Equal(reconciliationId, delivery.DeliveryAgencyReconciliationId);
        Assert.Equal(20m, delivery.AmountCollectedUsd);
        Assert.Equal(170m, delivery.ChangeGivenNio);
        Assert.Equal(36m, delivery.CollectionExchangeRate);
        Assert.Equal(80m, delivery.ShippingPaidToAgency);
        Assert.Equal((int)SalePaymentStatusOption.Paid, sale.SalePaymentStatusId);
        Assert.Contains(sale.PaymentMovements, item =>
            item.DeliveryAgencyReconciliationId == reconciliationId &&
            item.ProductAmount == 500m &&
            item.ShippingAmount == 50m &&
            item.GrossAmount == 550m);
        Assert.Equal(2, financialMovements.Count);
        Assert.Contains(financialMovements, item => item.MovementDirectionId == (int)MovementDirectionOptions.In && item.Amount == 720m);
        Assert.Contains(financialMovements, item => item.MovementDirectionId == (int)MovementDirectionOptions.Out && item.Amount == 250m);
    }

    [Fact]
    public async Task CreateAgencyReconciliationAsync_AllowsShippingCollectionWhenSaleIsRefundPending()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        context.Departments.Add(new Department { Id = 1, Name = "Managua" });
        context.Municipalities.Add(new Municipality { Id = 1, Name = "Managua", DepartmentId = 1 });
        await context.SaveChangesAsync();
        var expensiveProduct = await AddProductAsync(context, availableQuantity: 1, salePrice: 500m, unitCostNio: 200m);
        var cheaperProduct = await AddProductAsync(context, availableQuantity: 1, salePrice: 300m, unitCostNio: 120m);
        var saleService = CreateService(context);
        var reconciliationService = new DeliveryAgencyReconciliationService(context, new TestCurrentUserService());
        var saleId = await saleService.CreateAsync(new CreateSaleDTO
        {
            SaleChannelId = (int)SaleChannelOption.Whatsapp,
            Products = [new CreateSaleProductDTO { ProductId = expensiveProduct.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }],
            PaymentMovements = [new CreateSalePaymentMovementDTO { PaymentMethodId = (int)PaymentMethodOption.Transfer, ProductAmount = 500m }]
        });
        var deliveryId = await saleService.CreateDeliveryAsync(saleId, new CreateSaleDeliveryDTO
        {
            Code = "DEL-RECON-REFUND",
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            ShippingChargedToClient = 50m
        });
        await saleService.ReplaceProductsAsync(saleId, new ReplaceSaleProductsDTO
        {
            Products = [new CreateSaleProductDTO { ProductId = cheaperProduct.Id, Quantity = 1, DiscountSourceId = (int)DiscountSourceOption.None }]
        });
        await saleService.MarkDeliveryAsSentAsync(saleId, deliveryId);
        await saleService.MarkDeliveryAsCompletedAsync(saleId, deliveryId);

        var reconciliationId = await reconciliationService.CreateAsync(new CreateDeliveryAgencyReconciliationDTO
        {
            DeliveryAgencyId = 1,
            SettlementExchangeRate = 36m,
            Deliveries =
            [
                new ReconcileSaleDeliveryDTO
                {
                    SaleDeliveryId = deliveryId,
                    AmountCollectedNio = 50m,
                    ShippingPaidToAgency = 25m
                }
            ]
        });

        var sale = await context.Sales.Include(item => item.PaymentMovements).SingleAsync(item => item.Id == saleId);
        Assert.Equal((int)SalePaymentStatusOption.RefundPending, sale.SalePaymentStatusId);
        Assert.Contains(sale.PaymentMovements, item =>
            item.DeliveryAgencyReconciliationId == reconciliationId &&
            item.ProductAmount == 0m &&
            item.ShippingAmount == 50m);
    }

    [Fact]
    public async Task CreateAgencyReconciliationAsync_RejectsNullDeliveries()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var reconciliationService = new DeliveryAgencyReconciliationService(context, new TestCurrentUserService());
        var request = JsonSerializer.Deserialize<CreateDeliveryAgencyReconciliationDTO>(
            """
            {
              "deliveryAgencyId": 1,
              "settlementExchangeRate": 36,
              "deliveries": null
            }
            """)!;

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => reconciliationService.CreateAsync(request));

        Assert.Contains("al menos un envio", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static SaleService CreateService(ApplicationDbContext context)
    {
var currentUser = new TestCurrentUserService();
        var deliveryService = new SaleDeliveryService(context, currentUser);
        var paymentService = new SalePaymentMovementService(context, currentUser, deliveryService);
        return new SaleService(context, currentUser, paymentService, deliveryService);
    }

    private static async Task<Product> AddProductAsync(ApplicationDbContext context, int availableQuantity, decimal salePrice, decimal unitCostNio)
    {
        var product = new Product
        {
            OrderId = 1,
            ProductDetailId = 1,
            SizeId = 1,
            Quantity = availableQuantity,
            ReceivedQuantity = availableQuantity,
            AvailableQuantity = availableQuantity,
            UnitCostNio = unitCostNio,
            SalePrice = salePrice
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return product;
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext context)
    {
        context.SaleChannels.AddRange(
            new SaleChannel { Id = (int)SaleChannelOption.InStoreSale, Name = nameof(SaleChannelOption.InStoreSale) },
            new SaleChannel { Id = (int)SaleChannelOption.Whatsapp, Name = nameof(SaleChannelOption.Whatsapp) });
        context.SaleStatuses.AddRange(
            new SaleStatus { Id = (int)SaleStatusOption.Pending, Name = nameof(SaleStatusOption.Pending) },
            new SaleStatus { Id = (int)SaleStatusOption.Reserved, Name = nameof(SaleStatusOption.Reserved) },
            new SaleStatus { Id = (int)SaleStatusOption.ReadyForDelivery, Name = nameof(SaleStatusOption.ReadyForDelivery) },
            new SaleStatus { Id = (int)SaleStatusOption.SentForDelivery, Name = nameof(SaleStatusOption.SentForDelivery) },
            new SaleStatus { Id = (int)SaleStatusOption.Completed, Name = nameof(SaleStatusOption.Completed) },
            new SaleStatus { Id = (int)SaleStatusOption.Cancelled, Name = nameof(SaleStatusOption.Cancelled) });
        context.SalePaymentStatuses.AddRange(
            new SalePaymentStatus { Id = (int)SalePaymentStatusOption.Unpaid, Name = nameof(SalePaymentStatusOption.Unpaid) },
            new SalePaymentStatus { Id = (int)SalePaymentStatusOption.PartiallyPaid, Name = nameof(SalePaymentStatusOption.PartiallyPaid) },
            new SalePaymentStatus { Id = (int)SalePaymentStatusOption.Paid, Name = nameof(SalePaymentStatusOption.Paid) },
            new SalePaymentStatus { Id = (int)SalePaymentStatusOption.RefundPending, Name = nameof(SalePaymentStatusOption.RefundPending) });
        context.SaleProductStatuses.AddRange(
            new SaleProductStatus { Id = (int)SaleProductStatusOption.Pending, Name = nameof(SaleProductStatusOption.Pending) },
            new SaleProductStatus { Id = (int)SaleProductStatusOption.Completed, Name = nameof(SaleProductStatusOption.Completed) },
            new SaleProductStatus { Id = (int)SaleProductStatusOption.Cancelled, Name = nameof(SaleProductStatusOption.Cancelled) });
        context.DeliveryStatuses.AddRange(
            new DeliveryStatus { Id = (int)DeliveryStatusCode.Pending, Name = nameof(DeliveryStatusCode.Pending) },
            new DeliveryStatus { Id = (int)DeliveryStatusCode.Completed, Name = nameof(DeliveryStatusCode.Completed) },
            new DeliveryStatus { Id = (int)DeliveryStatusCode.Cancelled, Name = nameof(DeliveryStatusCode.Cancelled) },
            new DeliveryStatus { Id = (int)DeliveryStatusCode.Sent, Name = nameof(DeliveryStatusCode.Sent) },
            new DeliveryStatus { Id = (int)DeliveryStatusCode.Failed, Name = nameof(DeliveryStatusCode.Failed) });
        context.DiscountSources.Add(new DiscountSource { Id = (int)DiscountSourceOption.None, Name = nameof(DiscountSourceOption.None) });
        context.PaymentMethods.AddRange(
            new PaymentMethod { Id = (int)PaymentMethodOption.Cash, Name = "Efectivo" },
            new PaymentMethod { Id = (int)PaymentMethodOption.Transfer, Name = "Transferencia" },
            new PaymentMethod { Id = (int)PaymentMethodOption.Card, Name = "Tarjeta" });
        context.PaymentTerminals.Add(new PaymentTerminal
        {
            Id = 1,
            Name = "POS BAC",
            ComissionPercentage = 5m,
            IncomeTaxPercentage = 1m,
            Enabled = true
        });
        context.DeliveryAgencies.Add(new DeliveryAgency { Id = 1, Name = "Agencia COD", PhoneNumber = "88880000", CanCollectCashOnDelivery = true });
        context.FinancialMovementTypes.Add(new FinancialMovementType
        {
            Id = (int)FinancialMovementTypeOption.DeliveryAgencyReconciliation,
            Name = nameof(FinancialMovementTypeOption.DeliveryAgencyReconciliation)
        });

        context.DollarExchangeRates.Add(new DollarExchangeRate
        {
            Id = 1,
            BankRate = 36.5m,
            StartDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
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

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public string? UserId => "test-user";
    }
}
