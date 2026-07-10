using Microsoft.EntityFrameworkCore;
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
            Products =
            [
                new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    DiscountSourceId = (int)DiscountSourceOption.None
                }
            ],
            Payments =
            [
                new CreateSalePaymentDTO
                {
                    PaymentDate = new DateTime(2026, 7, 10, 10, 1, 0, DateTimeKind.Utc),
                    PaymentMethodId = 1,
                    GrossAmount = 500m
                },
                new CreateSalePaymentDTO
                {
                    PaymentDate = new DateTime(2026, 7, 10, 10, 2, 0, DateTimeKind.Utc),
                    PaymentMethodId = 2,
                    PaymentTerminalId = 1,
                    GrossAmount = 500m
                }
            ]
        });

        var sale = await context.Sales
            .Include(item => item.Products)
            .Include(item => item.Payments)
            .SingleAsync(item => item.Id == saleId);
        var updatedProduct = await context.Products.SingleAsync(item => item.Id == product.Id);
        var movements = await context.FinancialMovements.OrderBy(item => item.Id).ToListAsync();

        Assert.Equal((int)SalePaymentStatusOption.Paid, sale.SalePaymentStatusId);
        Assert.Equal(1000m, sale.Subtotal);
        Assert.Equal(1000m, sale.Total);
        Assert.Equal(2, updatedProduct.AvailableQuantity);
        Assert.Equal(2, sale.Payments.Count);
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
            Payments =
            [
                new CreateSalePaymentDTO
                {
                    PaymentMethodId = 1,
                    GrossAmount = 500m
                }
            ]
        }));

        Assert.Contains("ventas en local", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await context.Sales.AnyAsync());
    }

    private static SaleService CreateService(ApplicationDbContext context)
    {
        return new SaleService(context, new TestCurrentUserService());
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
        context.SaleChannels.Add(new SaleChannel { Id = (int)SaleChannelOption.InStoreSale, Name = nameof(SaleChannelOption.InStoreSale) });
        context.DiscountSources.Add(new DiscountSource { Id = (int)DiscountSourceOption.None, Name = nameof(DiscountSourceOption.None) });
        context.PaymentMethods.AddRange(
            new PaymentMethod { Id = 1, Name = "Efectivo" },
            new PaymentMethod { Id = 2, Name = "Tarjeta" });
        context.PaymentTerminals.Add(new PaymentTerminal
        {
            Id = 1,
            Name = "POS BAC",
            ComissionPercentage = 5m,
            IncomeTaxPercentage = 1m,
            Enabled = true
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






