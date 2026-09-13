using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Finances;

public class ExchangeRateServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_ReturnsLatestEnabledRateWithStoreAndBankValues()
    {
        await using var context = CreateContext();
        context.DollarExchangeRates.AddRange(
            new DollarExchangeRate
            {
                Id = 1,
                BankRate = 36.25m,
                StoreRate = 36.75m,
                StartDate = new DateTime(2026, 1, 1),
                Enabled = true
            },
            new DollarExchangeRate
            {
                Id = 2,
                BankRate = 36.50m,
                StoreRate = 37m,
                StartDate = new DateTime(2026, 2, 1),
                Enabled = true
            },
            new DollarExchangeRate
            {
                Id = 3,
                BankRate = 37m,
                StoreRate = 37.50m,
                StartDate = new DateTime(2026, 3, 1),
                Enabled = false
            });
        await context.SaveChangesAsync();
        var service = new ExchangeRateService(context);

        var result = await service.GetCurrentAsync();

        Assert.Equal(36.50m, result.BankRate);
        Assert.Equal(37m, result.StoreRate);
        Assert.Equal(new DateTime(2026, 2, 1), result.StartDate);
    }

    [Fact]
    public async Task GetCurrentAsync_ThrowsNotFoundWhenNoEnabledRateExists()
    {
        await using var context = CreateContext();
        context.DollarExchangeRates.Add(new DollarExchangeRate
        {
            BankRate = 36.50m,
            StoreRate = 37m,
            StartDate = new DateTime(2026, 2, 1),
            Enabled = false
        });
        await context.SaveChangesAsync();
        var service = new ExchangeRateService(context);

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => service.GetCurrentAsync());

        Assert.Equal("No existe una tasa de cambio bancaria habilitada.", exception.Message);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
