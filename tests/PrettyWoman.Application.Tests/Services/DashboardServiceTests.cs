using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Dashboard;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_IncludesExchangeDifferenceInCollections()
    {
        await using var context = CreateContext();
        var cash = new PaymentMethod { Id = (int)PaymentMethodOption.Cash, Name = "Efectivo" };
        context.PaymentMethods.Add(cash);
        context.SalePaymentMovements.Add(new SalePaymentMovement
        {
            SaleId = 1,
            MovementDate = new DateTime(2026, 7, 20, 18, 0, 0, DateTimeKind.Utc),
            MovementDirectionId = (int)MovementDirectionOptions.In,
            PaymentMethodId = cash.Id,
            PaymentMethod = cash,
            GrossAmount = 400m,
            ProductAmount = 400m,
            NetReceivedAmount = 400m,
            AmountReceivedUsd = 10.96m,
            ExchangeRate = 36.51m,
            ExchangeDifferenceNio = 0.15m,
            UserId = "test-user"
        });
        await context.SaveChangesAsync();

        var summary = await new DashboardService(context).GetSummaryAsync(
            new DashboardSummaryQueryDTO
            {
                FromDate = new DateOnly(2026, 7, 20),
                ToDate = new DateOnly(2026, 7, 20)
            },
            includeFinancialSummary: false);

        Assert.Equal(400.15m, summary.Payments.CollectedNio);
        var paymentMethod = Assert.Single(summary.Payments.ByPaymentMethod);
        Assert.Equal(cash.Id, paymentMethod.PaymentMethodId);
        Assert.Equal(400.15m, paymentMethod.CollectedNio);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
