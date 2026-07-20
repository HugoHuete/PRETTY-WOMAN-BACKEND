using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Dashboard;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class DashboardService(IApplicationDbContext context) : IDashboardService
{
    private static readonly TimeZoneInfo NicaraguaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
    private readonly IApplicationDbContext _context = context;

    public async Task<DashboardSummaryDTO> GetSummaryAsync(
        DashboardSummaryQueryDTO query,
        bool includeFinancialSummary,
        CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate, fromUtc, toUtc) = ResolvePeriod(query);

        var sales = await _context.Sales
            .AsNoTracking()
            .Where(sale => sale.SaleDate >= fromUtc && sale.SaleDate < toUtc)
            .Where(sale => sale.SaleStatusId != (int)SaleStatusOption.Cancelled)
            .Select(sale => new
            {
                sale.Total,
                NetPaid = sale.PaymentMovements.Sum(payment =>
                    payment.MovementDirectionId == (int)MovementDirectionOptions.In
                        ? payment.GrossAmount
                        : -payment.GrossAmount)
            })
            .ToListAsync(cancellationToken);

        var paymentMethods = await _context.SalePaymentMovements
            .AsNoTracking()
            .Where(payment => payment.MovementDate >= fromUtc && payment.MovementDate < toUtc)
            .Where(payment => payment.MovementDirectionId == (int)MovementDirectionOptions.In)
            .GroupBy(payment => new { payment.PaymentMethodId, PaymentMethodName = payment.PaymentMethod!.Name })
            .Select(group => new DashboardPaymentMethodSummaryDTO
            {
                PaymentMethodId = group.Key.PaymentMethodId,
                PaymentMethodName = group.Key.PaymentMethodName,
                // Cobros representa el ingreso financiero real; GrossAmount permanece
                // reservado para el saldo comercial aplicado a la venta.
                CollectedNio = group.Sum(payment => payment.NetReceivedAmount + payment.ExchangeDifferenceNio)
            })
            .OrderBy(summary => summary.PaymentMethodName)
            .ToListAsync(cancellationToken);

        var activeReservations = await _context.ProductHolds
            .AsNoTracking()
            .Where(hold => hold.HoldDate >= fromUtc && hold.HoldDate < toUtc)
            .Where(hold => hold.ProductHoldStatusId == (int)ProductHoldStatusOption.Active)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Units = group.Sum(hold => hold.Quantity)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var deliveries = await _context.SaleDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.CreatedAt >= fromUtc && delivery.CreatedAt < toUtc)
            .GroupBy(delivery => new { delivery.DeliveryStatusId, DeliveryStatusName = delivery.DeliveryStatus!.Name })
            .Select(group => new DashboardDeliveryStatusSummaryDTO
            {
                DeliveryStatusId = group.Key.DeliveryStatusId,
                DeliveryStatusName = group.Key.DeliveryStatusName,
                Count = group.Count()
            })
            .OrderBy(summary => summary.DeliveryStatusId)
            .ToListAsync(cancellationToken);

        var openIssues = await _context.ProductInventoryIssues
            .AsNoTracking()
            .Where(issue => issue.IssueDate >= fromUtc && issue.IssueDate < toUtc)
            .Where(issue => issue.ProductInventoryIssueStatusId == (int)ProductInventoryIssueStatusOption.Open)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Units = group.Sum(issue => issue.Quantity)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var result = new DashboardSummaryDTO
        {
            FromDate = fromDate,
            ToDate = toDate,
            Sales = new DashboardSalesSummaryDTO
            {
                Count = sales.Count,
                TotalNio = sales.Sum(sale => sale.Total),
                PendingCollectionNio = sales.Sum(sale => Math.Max(0, sale.Total - sale.NetPaid))
            },
            Payments = new DashboardPaymentsSummaryDTO
            {
                CollectedNio = paymentMethods.Sum(payment => payment.CollectedNio),
                ByPaymentMethod = paymentMethods
            },
            Operations = new DashboardOperationalSummaryDTO
            {
                ActiveReservationCount = activeReservations?.Count ?? 0,
                ActiveReservedUnitCount = activeReservations?.Units ?? 0,
                DeliveriesByStatus = deliveries,
                OpenInventoryIssueCount = openIssues?.Count ?? 0,
                OpenInventoryIssueUnitCount = openIssues?.Units ?? 0
            }
        };

        if (includeFinancialSummary)
        {
            var financial = await _context.FinancialMovements
                .AsNoTracking()
                .Where(movement => movement.MovementDate >= fromUtc && movement.MovementDate < toUtc)
                .GroupBy(_ => 1)
                .Select(group => new DashboardFinancialSummaryDTO
                {
                    IncomeNio = group
                        .Where(movement => movement.MovementDirectionId == (int)MovementDirectionOptions.In)
                        .Sum(movement => movement.Amount),
                    ExpenseNio = group
                        .Where(movement => movement.MovementDirectionId == (int)MovementDirectionOptions.Out)
                        .Sum(movement => movement.Amount)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new DashboardFinancialSummaryDTO();

            financial.BalanceNio = financial.IncomeNio - financial.ExpenseNio;
            result.Financial = financial;
        }

        return result;
    }

    private static (DateOnly FromDate, DateOnly ToDate, DateTime FromUtc, DateTime ToUtc) ResolvePeriod(DashboardSummaryQueryDTO query)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, NicaraguaTimeZone));
        var fromDate = query.FromDate ?? today;
        var toDate = query.ToDate ?? today;

        if (fromDate > toDate)
        {
            throw new AppBadRequestException("La fecha inicial no puede ser mayor que la fecha final.");
        }

        if (toDate == DateOnly.MaxValue)
        {
            throw new AppBadRequestException("La fecha final no puede ser 9999-12-31.");
        }

        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(fromDate.ToDateTime(TimeOnly.MinValue), NicaraguaTimeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), NicaraguaTimeZone);
        return (fromDate, toDate, fromUtc, toUtc);
    }
}
