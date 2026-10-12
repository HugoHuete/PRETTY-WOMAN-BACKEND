using Microsoft.EntityFrameworkCore;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Infrastructure.Tests.Persistence;

public class FinancialSummaryQueryTranslationTests
{
    [Fact]
    public void MonthlySalesAggregate_TranslatesManaguaTimezoneConversionToPostgreSql()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=pretty_woman_model_tests;Username=test;Password=test")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var context = new ApplicationDbContext(options);
        var query = context.Sales.AsNoTracking()
            .Select(sale => new
            {
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(sale.SaleDate, "America/Managua"),
                Amount = sale.Total
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.Amount)
            });

        var sql = query.ToQueryString();

        Assert.Contains("AT TIME ZONE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("America/Managua", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void MonthlyAggregateWithNullableAndRelatedDates_TranslatesManaguaTimezoneConversionToPostgreSql()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=pretty_woman_model_tests;Username=test;Password=test")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var context = new ApplicationDbContext(options);
        var refundQuery = context.SaleReturns.AsNoTracking()
            .Where(saleReturn => saleReturn.RefundedAt.HasValue)
            .Select(saleReturn => new
            {
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(saleReturn.RefundedAt!.Value, "America/Managua"),
                Amount = saleReturn.ProductRefundTotal
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(item => item.Amount) });

        var deliveryQuery = context.SaleDeliveries.AsNoTracking()
            .Where(delivery => delivery.DeliveryAgencyReconciliation != null)
            .Select(delivery => new
            {
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                    delivery.DeliveryAgencyReconciliation!.ReconciliationDate,
                    "America/Managua"),
                Amount = delivery.ShippingChargedToClient
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(item => item.Amount) });

        Assert.Contains("AT TIME ZONE", refundQuery.ToQueryString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AT TIME ZONE", deliveryQuery.ToQueryString(), StringComparison.OrdinalIgnoreCase);
    }
}
