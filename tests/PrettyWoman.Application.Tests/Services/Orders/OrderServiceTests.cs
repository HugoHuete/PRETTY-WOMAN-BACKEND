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
        Assert.Equal(6.85m, product.UnitCostUsd);
        Assert.Equal(432.5m, product.UnitCostNio);
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
        context.MovementDirections.Add(new MovementDirection { Id = (int)MovementDirectionOptions.Out, Name = "Out" });
        context.FinancialMovementTypes.Add(new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SupplierPayment, Name = "SupplierPayment" });
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
