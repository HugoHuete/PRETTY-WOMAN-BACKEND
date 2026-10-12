using System.Data;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Finances;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class FinancialService(IApplicationDbContext context) : IFinancialService
{
    private const string ManaguaTimeZoneId = "America/Managua";
    private readonly IApplicationDbContext _context = context;

    public async Task<CurrentFinancialBalanceDTO> GetCurrentBalanceAsync()
    {
        var balance = await _context.FinancialMovements
            .GroupBy(_ => 1)
            .Select(group => new CurrentFinancialBalanceDTO
            {
                IncomeTotalNio = group
                    .Where(movement => movement.MovementDirectionId == (int)MovementDirectionOptions.In)
                    .Sum(movement => movement.Amount),
                ExpenseTotalNio = group
                    .Where(movement => movement.MovementDirectionId == (int)MovementDirectionOptions.Out)
                    .Sum(movement => movement.Amount),
                MovementCount = group.Count(),
                LastMovementAt = group.Max(movement => (DateTime?)movement.MovementDate)
            })
            .FirstOrDefaultAsync();

        balance ??= new CurrentFinancialBalanceDTO();
        balance.BalanceNio = balance.IncomeTotalNio - balance.ExpenseTotalNio;

        return balance;
    }

    public async Task<MonthlyFinancialSummaryDTO> GetMonthlySummaryAsync(int? fromYear, int? toYear)
    {
        var totalsByMonth = await GetMonthlyTotalsAsync(fromYear, toYear);
        var years = totalsByMonth.Keys.Select(key => key.Year).Distinct().Order().ToList();
        var result = new MonthlyFinancialSummaryDTO { FromYear = fromYear, ToYear = toYear };

        foreach (var year in years)
        {
            var yearDto = new MonthlyFinancialSummaryYearDTO { Year = year };
            for (var month = 1; month <= 12; month++)
            {
                totalsByMonth.TryGetValue((year, month), out var totals);
                var monthDto = new MonthlyFinancialSummaryMonthDTO { Month = month };
                SetTotals(monthDto, totals);
                yearDto.Months.Add(monthDto);
            }

            SetTotals(yearDto, totalsByMonth.Where(pair => pair.Key.Year == year).Select(pair => pair.Value));
            result.Years.Add(yearDto);
        }

        SetTotals(result.GrandTotal, totalsByMonth.Values);
        return result;
    }

    public async Task<byte[]> ExportMonthlySummaryAsync(int? fromYear, int? toYear)
    {
        var totalsByMonth = await GetMonthlyTotalsAsync(fromYear, toYear);
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Resumen financiero");
        worksheet.Cell(1, 1).Value = "Resumen financiero detallado (C$)";
        worksheet.Range(1, 1, 1, 24).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;

        var headers = new[]
        {
            "Año", "Mes", "Ventas brutas", "Comisiones de tarjeta", "Impuestos de tarjeta",
            "Devoluciones de productos", "Ingreso neto por cambios", "Ingresos netos",
            "Ganancia de productos", "Conciliación de envíos", "Conciliación de devoluciones",
            "Reembolso de envío original", "Ajuste por devolución recibida", "Ganancia de cambios",
            "Ajuste por devolución de cambio", "Ganancia bruta", "Gastos manuales",
            "Intereses de préstamos", "Incidencias perdidas", "Faltantes de compras",
            "Reembolsos de proveedor", "Gastos totales", "Utilidad", "% Utilidad"
        };
        for (var column = 1; column <= headers.Length; column++)
            worksheet.Cell(2, column).Value = headers[column - 1];

        var header = worksheet.Range(2, 1, 2, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
        header.Style.Alignment.WrapText = true;

        var row = 3;
        var years = totalsByMonth.Keys.Select(key => key.Year).Distinct().Order().ToList();
        foreach (var year in years)
        {
            var yearTotals = totalsByMonth.Where(pair => pair.Key.Year == year).Select(pair => pair.Value).ToList();
            for (var month = 1; month <= 12; month++)
            {
                totalsByMonth.TryGetValue((year, month), out var totals);
                WriteMonthlySummaryRow(worksheet, row++, year, month, totals);
            }

            WriteMonthlySummaryTotalRow(worksheet, row++, $"{year} Total", yearTotals);
        }

        WriteMonthlySummaryTotalRow(worksheet, row, "Total general", totalsByMonth.Values);
        worksheet.Range(3, 3, row, 23).Style.NumberFormat.Format = "#,##0.00;[Red]-#,##0.00;–";
        worksheet.Range(3, 24, row, 24).Style.NumberFormat.Format = "0.00\"%\"";
        worksheet.Range(3, 1, row, headers.Length).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
        worksheet.SheetView.FreezeRows(2);
        worksheet.Columns().AdjustToContents();
        worksheet.Column(1).Width = 12;
        worksheet.Column(2).Width = 18;
        worksheet.Row(2).Height = 34;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<Dictionary<(int Year, int Month), MonthlyTotalsAccumulator>> GetMonthlyTotalsAsync(int? fromYear, int? toYear)
    {
        if (fromYear.HasValue && toYear.HasValue && fromYear.Value > toYear.Value)
            throw new AppBadRequestException("El año inicial no puede ser mayor que el año final.");

        if (fromYear is < 1 or > 9999 || toYear is < 1 or > 9999)
            throw new AppBadRequestException("Los años deben estar entre 1 y 9999.");

        var currentManaguaYear = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, ManaguaTimeZoneId).Year;
        var effectiveToYear = toYear ?? currentManaguaYear;
        var managuaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ManaguaTimeZoneId);

        DateTime? startUtc = fromYear.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(new DateTime(fromYear.Value, 1, 1), managuaTimeZone)
            : null;

        DateTime? endUtcExclusive = effectiveToYear < 9999
            ? TimeZoneInfo.ConvertTimeToUtc(new DateTime(effectiveToYear + 1, 1, 1), managuaTimeZone)
            : null;

        await using var transaction = await _context.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var totalsByMonth = new Dictionary<(int Year, int Month), MonthlyTotalsAccumulator>();

        MonthlyTotalsAccumulator GetAccumulator(int year, int month)
        {
            var key = (year, month);
            if (!totalsByMonth.TryGetValue(key, out var accumulator))
            {
                accumulator = new MonthlyTotalsAccumulator();
                totalsByMonth.Add(key, accumulator);
            }

            return accumulator;
        }

        var sales = _context.Sales.AsNoTracking()
            .Where(sale => sale.SaleStatusId != (int)SaleStatusOption.Cancelled &&
                           (!startUtc.HasValue || sale.SaleDate >= startUtc.Value) &&
                           (!endUtcExclusive.HasValue || sale.SaleDate < endUtcExclusive.Value));

        var salesByMonth = await sales
            .Select(sale => new
            {
                Sale = sale,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(sale.SaleDate, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(item => item.Sale.Total) })
            .ToListAsync();

        foreach (var item in salesByMonth)
            GetAccumulator(item.Year, item.Month).SalesNio += item.Amount;

        var paymentFeesByMonth = await _context.SalePaymentMovements.AsNoTracking()
            .Where(payment => payment.MovementDirectionId == (int)MovementDirectionOptions.In &&
                             payment.Sale != null && payment.Sale.SaleStatusId != (int)SaleStatusOption.Cancelled &&
                             (!startUtc.HasValue || payment.MovementDate >= startUtc.Value) &&
                             (!endUtcExclusive.HasValue || payment.MovementDate < endUtcExclusive.Value))
            .Select(payment => new
            {
                Payment = payment,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(payment.MovementDate, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                CommissionAmount = group.Sum(item => item.Payment.CommissionAmount),
                IncomeTaxAmount = group.Sum(item => item.Payment.IncomeTaxAmount)
            })
            .ToListAsync();

        foreach (var item in paymentFeesByMonth)
        {
            var accumulator = GetAccumulator(item.Year, item.Month);
            accumulator.CardCommissionNio -= item.CommissionAmount;
            accumulator.CardIncomeTaxNio -= item.IncomeTaxAmount;
        }

        var saleProfitsByMonth = await _context.SaleProducts.AsNoTracking()
            .Where(item => item.Sale != null && item.Sale.SaleStatusId != (int)SaleStatusOption.Cancelled &&
                           (!startUtc.HasValue || item.Sale.SaleDate >= startUtc.Value) &&
                           (!endUtcExclusive.HasValue || item.Sale.SaleDate < endUtcExclusive.Value))
            .Select(item => new
            {
                Product = item,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Sale!.SaleDate, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(item => item.Product.GrossProfit) })
            .ToListAsync();

        foreach (var item in saleProfitsByMonth)
            GetAccumulator(item.Year, item.Month).ProductSalesGrossProfitNio += item.Amount;

        var refundsByMonth = await _context.SaleReturns.AsNoTracking()
            .Where(saleReturn => saleReturn.StatusId != (int)SaleReturnStatusOption.Cancelled && saleReturn.RefundedAt.HasValue &&
                                 (!startUtc.HasValue || saleReturn.RefundedAt.Value >= startUtc.Value) &&
                                 (!endUtcExclusive.HasValue || saleReturn.RefundedAt.Value < endUtcExclusive.Value))
            .Select(saleReturn => new
            {
                SaleReturn = saleReturn,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(saleReturn.RefundedAt!.Value, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(item => item.SaleReturn.ProductRefundTotal) })
            .ToListAsync();

        foreach (var item in refundsByMonth)
            GetAccumulator(item.Year, item.Month).ProductRefundsNio -= item.Amount;

        var deliveryShippingMarginsByMonth = await _context.SaleDeliveries.AsNoTracking()
            .Where(delivery => delivery.DeliveryAgencyReconciliation != null &&
                               (!startUtc.HasValue || delivery.DeliveryAgencyReconciliation.ReconciliationDate >= startUtc.Value) &&
                               (!endUtcExclusive.HasValue || delivery.DeliveryAgencyReconciliation.ReconciliationDate < endUtcExclusive.Value))
            .Select(delivery => new
            {
                Delivery = delivery,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(delivery.DeliveryAgencyReconciliation!.ReconciliationDate, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.Delivery.ShippingChargedToClient - item.Delivery.ShippingPaidToAgency)
            })
            .ToListAsync();
        foreach (var item in deliveryShippingMarginsByMonth)
            GetAccumulator(item.Year, item.Month).SaleShippingReconciliationNio += item.Amount;

        var returnShippingMarginsByMonth = await _context.SaleReturns.AsNoTracking()
            .Where(saleReturn => saleReturn.DeliveryAgencyReconciliation != null &&
                                 saleReturn.StatusId != (int)SaleReturnStatusOption.Cancelled &&
                                 (!startUtc.HasValue || saleReturn.DeliveryAgencyReconciliation.ReconciliationDate >= startUtc.Value) &&
                                 (!endUtcExclusive.HasValue || saleReturn.DeliveryAgencyReconciliation.ReconciliationDate < endUtcExclusive.Value))
            .Select(saleReturn => new
            {
                SaleReturn = saleReturn,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(saleReturn.DeliveryAgencyReconciliation!.ReconciliationDate, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.SaleReturn.ReturnShippingChargedToClient - item.SaleReturn.ReturnShippingPaidToAgency)
            })
            .ToListAsync();
        foreach (var item in returnShippingMarginsByMonth)
            GetAccumulator(item.Year, item.Month).ReturnShippingReconciliationNio += item.Amount;

        var originalShippingRefundsByMonth = await _context.SaleReturns.AsNoTracking()
            .Where(saleReturn => saleReturn.StatusId != (int)SaleReturnStatusOption.Cancelled && saleReturn.RefundedAt.HasValue &&
                                 (!startUtc.HasValue || saleReturn.RefundedAt.Value >= startUtc.Value) &&
                                 (!endUtcExclusive.HasValue || saleReturn.RefundedAt.Value < endUtcExclusive.Value))
            .Select(saleReturn => new
            {
                SaleReturn = saleReturn,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(saleReturn.RefundedAt!.Value, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.SaleReturn.OriginalShippingRefund)
            })
            .ToListAsync();
        foreach (var item in originalShippingRefundsByMonth)
            GetAccumulator(item.Year, item.Month).OriginalShippingRefundNio -= item.Amount;

        var receivedReturnAdjustmentsByMonth = await _context.SaleReturnItems.AsNoTracking()
            .Where(item => item.SaleReturn != null && item.SaleReturn.StatusId != (int)SaleReturnStatusOption.Cancelled && item.ReceivedAt.HasValue &&
                           (!startUtc.HasValue || item.ReceivedAt.Value >= startUtc.Value) &&
                           (!endUtcExclusive.HasValue || item.ReceivedAt.Value < endUtcExclusive.Value))
            .Select(item => new
            {
                ReturnItem = item,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.ReceivedAt!.Value, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => (item.ReturnItem.OriginalUnitCost - item.ReturnItem.RecognizedUnitAmount) * item.ReturnItem.Quantity)
            })
            .ToListAsync();

        foreach (var item in receivedReturnAdjustmentsByMonth)
            GetAccumulator(item.Year, item.Month).ReceivedReturnAdjustmentNio += item.Amount;

        var completedExchangesByMonth = await _context.SaleExchanges.AsNoTracking()
            .Where(exchange => exchange.StatusId == (int)SaleExchangeStatusOption.Completed && exchange.CompletedAt.HasValue &&
                               (!startUtc.HasValue || exchange.CompletedAt.Value >= startUtc.Value) &&
                               (!endUtcExclusive.HasValue || exchange.CompletedAt.Value < endUtcExclusive.Value))
            .Select(exchange => new
            {
                Exchange = exchange,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(exchange.CompletedAt!.Value, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.Exchange.OutboundItemsTotal - item.Exchange.RecognizedReturnTotal)
            })
            .ToListAsync();

        foreach (var item in completedExchangesByMonth)
            GetAccumulator(item.Year, item.Month).ExchangeIncomeNio += item.Amount;

        var exchangeOutboundProfitsByMonth = await _context.ExchangeOutboundItems.AsNoTracking()
            .Where(item => item.SaleExchange != null && item.SaleExchange.StatusId == (int)SaleExchangeStatusOption.Completed && item.SaleExchange.CompletedAt.HasValue &&
                           (!startUtc.HasValue || item.SaleExchange.CompletedAt.Value >= startUtc.Value) &&
                           (!endUtcExclusive.HasValue || item.SaleExchange.CompletedAt.Value < endUtcExclusive.Value))
            .Select(item => new
            {
                OutboundItem = item,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.SaleExchange!.CompletedAt!.Value, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.OutboundItem.LineTotal - item.OutboundItem.TotalCost)
            })
            .ToListAsync();

        foreach (var item in exchangeOutboundProfitsByMonth)
            GetAccumulator(item.Year, item.Month).ExchangeOutboundProfitNio += item.Amount;

        var exchangeReturnAdjustmentsByMonth = await _context.ExchangeReturnItems.AsNoTracking()
            .Where(item => item.SaleExchange != null && item.SaleExchange.StatusId == (int)SaleExchangeStatusOption.Completed && item.SaleExchange.CompletedAt.HasValue &&
                           (!startUtc.HasValue || item.SaleExchange.CompletedAt.Value >= startUtc.Value) &&
                           (!endUtcExclusive.HasValue || item.SaleExchange.CompletedAt.Value < endUtcExclusive.Value))
            .Select(item => new
            {
                ReturnItem = item,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.SaleExchange!.CompletedAt!.Value, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.ReturnItem.StatusId == (int)ExchangeReturnItemStatusOption.Received
                    ? (item.ReturnItem.OriginalUnitCost - item.ReturnItem.RecognizedUnitAmount) * item.ReturnItem.Quantity
                    : -item.ReturnItem.RecognizedUnitAmount * item.ReturnItem.Quantity)
            })
            .ToListAsync();
        foreach (var item in exchangeReturnAdjustmentsByMonth)
            GetAccumulator(item.Year, item.Month).ExchangeReturnAdjustmentNio += item.Amount;

        var expensesByMonth = await _context.FinancialMovements.AsNoTracking()
            .Where(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.Expense &&
                               (!startUtc.HasValue || movement.MovementDate >= startUtc.Value) &&
                               (!endUtcExclusive.HasValue || movement.MovementDate < endUtcExclusive.Value))
            .Select(movement => new
            {
                Movement = movement,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(movement.MovementDate, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(item => item.Movement.Amount) })
            .ToListAsync();

        foreach (var item in expensesByMonth)
            GetAccumulator(item.Year, item.Month).ManualExpensesNio += item.Amount;

        var lostIssuesByMonth = await _context.ProductInventoryIssues.AsNoTracking()
            .Where(issue => issue.ProductInventoryIssueStatusId == (int)ProductInventoryIssueStatusOption.ConfirmedLost && issue.ResolvedAt.HasValue &&
                            (!startUtc.HasValue || issue.ResolvedAt.Value >= startUtc.Value) &&
                            (!endUtcExclusive.HasValue || issue.ResolvedAt.Value < endUtcExclusive.Value))
            .Select(issue => new
            {
                Issue = issue,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(issue.ResolvedAt!.Value, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.Issue.UnitCostNioAtIssue * item.Issue.Quantity)
            })
            .ToListAsync();

        foreach (var item in lostIssuesByMonth)
            GetAccumulator(item.Year, item.Month).LostInventoryIssuesNio += item.Amount;

        var loanInterestByMonth = await _context.LoanPayments.AsNoTracking()
            .Where(payment => (!startUtc.HasValue || payment.PaymentDate >= startUtc.Value) &&
                              (!endUtcExclusive.HasValue || payment.PaymentDate < endUtcExclusive.Value))
            .Select(payment => new
            {
                Payment = payment,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(payment.PaymentDate, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.Payment.InterestAmount)
            })
            .ToListAsync();

        foreach (var item in loanInterestByMonth)
            GetAccumulator(item.Year, item.Month).LoanInterestNio += item.Amount;

        var purchaseShortagesByMonth = await _context.PurchaseShortages.AsNoTracking()
            .Where(shortage => (!startUtc.HasValue || shortage.ShortageDate >= startUtc.Value) &&
                               (!endUtcExclusive.HasValue || shortage.ShortageDate < endUtcExclusive.Value))
            .Select(shortage => new
            {
                Shortage = shortage,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(shortage.ShortageDate, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.Shortage.LossAmountNio)
            })
            .ToListAsync();

        foreach (var item in purchaseShortagesByMonth)
            GetAccumulator(item.Year, item.Month).PurchaseShortageLossNio += item.Amount;

        var supplierRefundsByMonth = await _context.SupplierRefunds.AsNoTracking()
            .Where(refund => (!startUtc.HasValue || refund.RefundedAt >= startUtc.Value) &&
                             (!endUtcExclusive.HasValue || refund.RefundedAt < endUtcExclusive.Value))
            .Select(refund => new
            {
                Refund = refund,
                LocalDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(refund.RefundedAt, ManaguaTimeZoneId)
            })
            .GroupBy(item => new { item.LocalDate.Year, item.LocalDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(item => item.Refund.AmountNio)
            })
            .ToListAsync();

        foreach (var item in supplierRefundsByMonth)
            GetAccumulator(item.Year, item.Month).SupplierRefundsNio -= item.Amount;

        await transaction.CommitAsync();
        return totalsByMonth;
    }


    public async Task<IEnumerable<FinancialMovementTypeDTO>> GetMovementTypesAsync()
    {
        return await _context.FinancialMovementTypes
            .AsNoTracking()
            .OrderBy(type => type.Id)
            .Select(type => new FinancialMovementTypeDTO
            {
                Id = type.Id,
                Name = type.Name
            })
            .ToListAsync();
    }
    public async Task<PaginatedResult<FinancialMovementDTO>> GetMovementsAsync(FinancialMovementQueryDTO query)
    {
        NormalizePagination(query);

        var movementsQuery = _context.FinancialMovements
            .AsNoTracking()
            .Include(movement => movement.MovementDirection)
            .Include(movement => movement.FinancialMovementType)
            .AsQueryable();

        if (query.FromDate.HasValue)
        {
            movementsQuery = movementsQuery.Where(movement => movement.MovementDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            movementsQuery = movementsQuery.Where(movement => movement.MovementDate < query.ToDate.Value.AddDays(1));
        }

        if (query.FinancialMovementTypeId.HasValue)
        {
            movementsQuery = movementsQuery.Where(movement =>
                movement.FinancialMovementTypeId == query.FinancialMovementTypeId.Value);
        }

        if (query.MovementDirectionId.HasValue)
        {
            movementsQuery = movementsQuery.Where(movement =>
                movement.MovementDirectionId == query.MovementDirectionId.Value);
        }

        var totalCount = await movementsQuery.CountAsync();
        var items = await movementsQuery
            .OrderByDescending(movement => movement.MovementDate)
            .ThenByDescending(movement => movement.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(movement => MapMovementDTO(movement))
            .ToListAsync();

        return new PaginatedResult<FinancialMovementDTO>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<FinancialMovementDTO> CreateManualMovementAsync(CreateFinancialMovementDTO createMovementDTO)
    {
        NormalizeManualMovementFields(createMovementDTO.Description, createMovementDTO.Amount, createMovementDTO.Comments, out var description, out var comments);

        var movementType = (FinancialMovementTypeOption)createMovementDTO.FinancialMovementTypeId;
        EnsureManualMovementTypeIsAllowed(movementType);
        var movementDirectionId = ResolveManualMovementDirection(movementType, createMovementDTO.MovementDirectionId);
        await ValidateExpenseCategoryAsync(movementType, createMovementDTO.ExpenseCategoryId);
        var exchangeRate = await GetCurrentExchangeRateAsync();

        var movement = new FinancialMovement
        {
            Description = description,
            MovementDate = createMovementDTO.MovementDate ?? DateTime.UtcNow,
            MovementDirectionId = movementDirectionId,
            FinancialMovementTypeId = createMovementDTO.FinancialMovementTypeId,
            ExpenseCategoryId = movementType == FinancialMovementTypeOption.Expense ? createMovementDTO.ExpenseCategoryId : null,
            Amount = createMovementDTO.Amount,
            ExchangeRate = exchangeRate,
            Comments = comments
        };

        await _context.FinancialMovements.AddAsync(movement);
        await _context.SaveChangesAsync();

        return await GetMovementByIdAsync(movement.Id);
    }

    public async Task<FinancialMovementDTO> UpdateManualMovementAsync(int id, UpdateFinancialMovementDTO updateMovementDTO)
    {
        var movement = await _context.FinancialMovements
            .FirstOrDefaultAsync(movement => movement.Id == id)
            ?? throw new AppNotFoundException($"El movimiento financiero con id '{id}' no existe.");

        EnsureMovementCanBeManagedManually(movement);
        NormalizeManualMovementFields(updateMovementDTO.Description, updateMovementDTO.Amount, updateMovementDTO.Comments, out var description, out var comments);

        var movementType = (FinancialMovementTypeOption)updateMovementDTO.FinancialMovementTypeId;
        EnsureManualMovementTypeIsAllowed(movementType);
        var movementDirectionId = ResolveManualMovementDirection(movementType, updateMovementDTO.MovementDirectionId);
        await ValidateExpenseCategoryAsync(movementType, updateMovementDTO.ExpenseCategoryId);

        movement.Description = description;
        movement.MovementDate = updateMovementDTO.MovementDate ?? movement.MovementDate;
        movement.MovementDirectionId = movementDirectionId;
        movement.FinancialMovementTypeId = updateMovementDTO.FinancialMovementTypeId;
        movement.ExpenseCategoryId = movementType == FinancialMovementTypeOption.Expense ? updateMovementDTO.ExpenseCategoryId : null;
        movement.Amount = updateMovementDTO.Amount;
        movement.Comments = comments;

        await _context.SaveChangesAsync();

        return await GetMovementByIdAsync(movement.Id);
    }

    public async Task DeleteManualMovementAsync(int id)
    {
        var movement = await _context.FinancialMovements
            .FirstOrDefaultAsync(movement => movement.Id == id)
            ?? throw new AppNotFoundException($"El movimiento financiero con id '{id}' no existe.");

        EnsureMovementCanBeManagedManually(movement);

        _context.FinancialMovements.Remove(movement);
        await _context.SaveChangesAsync();
    }

    private async Task<FinancialMovementDTO> GetMovementByIdAsync(int id)
    {
        return await _context.FinancialMovements
            .AsNoTracking()
            .Include(movement => movement.MovementDirection)
            .Include(movement => movement.FinancialMovementType)
            .Where(movement => movement.Id == id)
            .Select(movement => MapMovementDTO(movement))
            .FirstAsync();
    }

    private static FinancialMovementDTO MapMovementDTO(FinancialMovement movement)
    {
        return new FinancialMovementDTO
        {
            Id = movement.Id,
            Description = movement.Description,
            MovementDate = movement.MovementDate,
            CreatedAt = movement.CreatedAt,
            MovementDirectionId = movement.MovementDirectionId,
            MovementDirectionName = movement.MovementDirection != null ? movement.MovementDirection.Name : null,
            FinancialMovementTypeId = movement.FinancialMovementTypeId,
            FinancialMovementTypeName = movement.FinancialMovementType != null ? movement.FinancialMovementType.Name : null,
            ExpenseCategoryId = movement.ExpenseCategoryId,
            OrderId = movement.OrderId,
            SalePaymentMovementId = movement.SalePaymentMovementId,
            DeliveryAgencyReconciliationId = movement.DeliveryAgencyReconciliationId,
            LoanId = movement.LoanId,
            LoanPaymentId = movement.LoanPaymentId,
            Amount = movement.Amount,
            ExchangeRate = movement.ExchangeRate,
            Comments = movement.Comments
        };
    }

    private static void NormalizePagination(FinancialMovementQueryDTO query)
    {
        if (query.Page <= 0)
        {
            throw new AppBadRequestException("La pagina debe ser mayor que cero.");
        }

        if (query.PageSize <= 0)
        {
            throw new AppBadRequestException("El tamano de pagina debe ser mayor que cero.");
        }

        if (query.PageSize > 100)
        {
            throw new AppBadRequestException("El tamano de pagina no puede ser mayor que 100.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
        {
            throw new AppBadRequestException("La fecha inicial no puede ser mayor que la fecha final.");
        }
    }

    private static void NormalizeManualMovementFields(
        string descriptionInput,
        decimal amount,
        string? commentsInput,
        out string description,
        out string? comments)
    {
        description = descriptionInput.NormalizeRequired("Descripcion del movimiento financiero");
        comments = commentsInput.NormalizeOptional();

        if (amount <= 0)
        {
            throw new AppBadRequestException("El monto debe ser mayor que cero.");
        }
    }

    private static void EnsureManualMovementTypeIsAllowed(FinancialMovementTypeOption movementType)
    {
        var isAllowed = movementType is FinancialMovementTypeOption.OwnerInvestment
            or FinancialMovementTypeOption.Expense
            or FinancialMovementTypeOption.OwnerWithdrawal
            or FinancialMovementTypeOption.Adjustment;

        if (!isAllowed)
        {
            throw new AppBadRequestException("Este tipo de movimiento financiero no se puede registrar manualmente desde finanzas.");
        }
    }

    private static void EnsureMovementCanBeManagedManually(FinancialMovement movement)
    {
        EnsureManualMovementTypeIsAllowed((FinancialMovementTypeOption)movement.FinancialMovementTypeId);

        if (movement.OrderId.HasValue || movement.SalePaymentMovementId.HasValue || movement.LoanId.HasValue || movement.LoanPaymentId.HasValue)
        {
            throw new AppBadRequestException("Este movimiento financiero esta relacionado con otro flujo y no se puede modificar manualmente desde finanzas.");
        }
    }

    private static int ResolveManualMovementDirection(FinancialMovementTypeOption movementType, int? requestedDirectionId)
    {
        if (movementType == FinancialMovementTypeOption.Adjustment)
        {
            if (requestedDirectionId is not ((int)MovementDirectionOptions.In or (int)MovementDirectionOptions.Out))
            {
                throw new AppBadRequestException("Los ajustes deben indicar una direccion de movimiento valida.");
            }

            return requestedDirectionId.Value;
        }

        return movementType switch
        {
            FinancialMovementTypeOption.OwnerInvestment => (int)MovementDirectionOptions.In,
            FinancialMovementTypeOption.Expense => (int)MovementDirectionOptions.Out,
            FinancialMovementTypeOption.OwnerWithdrawal => (int)MovementDirectionOptions.Out,
            _ => throw new AppBadRequestException("Tipo de movimiento financiero no soportado.")
        };
    }

    private async Task ValidateExpenseCategoryAsync(FinancialMovementTypeOption movementType, int? expenseCategoryId)
    {
        if (movementType != FinancialMovementTypeOption.Expense)
        {
            if (expenseCategoryId.HasValue)
            {
                throw new AppBadRequestException("Solo los movimientos de gasto pueden tener categoria de gasto.");
            }

            return;
        }

        if (!expenseCategoryId.HasValue)
        {
            throw new AppBadRequestException("Los gastos deben indicar una categoria de gasto.");
        }

        var exists = await _context.ExpenseCategories
            .AnyAsync(category => category.Id == expenseCategoryId.Value && category.Enabled);

        if (!exists)
        {
            throw new AppBadRequestException($"La categoria de gasto con id '{expenseCategoryId.Value}' no existe o no esta habilitada.");
        }
    }

    private async Task<decimal> GetCurrentExchangeRateAsync()
    {
        var exchangeRate = await _context.DollarExchangeRates
            .Where(rate => rate.Enabled)
            .OrderByDescending(rate => rate.StartDate)
            .Select(rate => (decimal?)rate.BankRate)
            .FirstOrDefaultAsync();

        return exchangeRate
            ?? throw new AppBadRequestException("Debe existir una tasa de cambio bancaria habilitada para registrar movimientos financieros.");
    }

    private static void SetTotals(FinancialSummaryTotalsDTO target, MonthlyTotalsAccumulator? totals)
    {
        target.IncomeNio = totals?.IncomeNio ?? 0;
        target.GrossProfitNio = totals?.GrossProfitNio ?? 0;
        target.ExpensesNio = totals?.ExpensesNio ?? 0;
        target.UtilityNio = target.GrossProfitNio - target.ExpensesNio;
        target.UtilityPercentage = target.IncomeNio == 0
            ? 0
            : Math.Round(target.UtilityNio / target.IncomeNio * 100, 2);
    }

    private static void SetTotals(FinancialSummaryTotalsDTO target, IEnumerable<MonthlyTotalsAccumulator> totals)
    {
        target.IncomeNio = totals.Sum(item => item.IncomeNio);
        target.GrossProfitNio = totals.Sum(item => item.GrossProfitNio);
        target.ExpensesNio = totals.Sum(item => item.ExpensesNio);
        target.UtilityNio = target.GrossProfitNio - target.ExpensesNio;
        target.UtilityPercentage = target.IncomeNio == 0
            ? 0
            : Math.Round(target.UtilityNio / target.IncomeNio * 100, 2);
    }

    private sealed class MonthlyTotalsAccumulator
    {
        public decimal SalesNio { get; set; }
        public decimal CardCommissionNio { get; set; }
        public decimal CardIncomeTaxNio { get; set; }
        public decimal ProductRefundsNio { get; set; }
        public decimal ExchangeIncomeNio { get; set; }
        public decimal ProductSalesGrossProfitNio { get; set; }
        public decimal SaleShippingReconciliationNio { get; set; }
        public decimal ReturnShippingReconciliationNio { get; set; }
        public decimal OriginalShippingRefundNio { get; set; }
        public decimal ReceivedReturnAdjustmentNio { get; set; }
        public decimal ExchangeOutboundProfitNio { get; set; }
        public decimal ExchangeReturnAdjustmentNio { get; set; }
        public decimal ManualExpensesNio { get; set; }
        public decimal LoanInterestNio { get; set; }
        public decimal LostInventoryIssuesNio { get; set; }
        public decimal PurchaseShortageLossNio { get; set; }
        public decimal SupplierRefundsNio { get; set; }

        public decimal IncomeNio => SalesNio + CardCommissionNio + CardIncomeTaxNio + ProductRefundsNio + ExchangeIncomeNio;
        public decimal GrossProfitNio => ProductSalesGrossProfitNio + CardCommissionNio + CardIncomeTaxNio + SaleShippingReconciliationNio +
                                         ReturnShippingReconciliationNio + OriginalShippingRefundNio + ReceivedReturnAdjustmentNio +
                                         ExchangeOutboundProfitNio + ExchangeReturnAdjustmentNio;
        public decimal ExpensesNio => ManualExpensesNio + LoanInterestNio + LostInventoryIssuesNio + PurchaseShortageLossNio + SupplierRefundsNio;

        public void Add(MonthlyTotalsAccumulator other)
        {
            SalesNio += other.SalesNio;
            CardCommissionNio += other.CardCommissionNio;
            CardIncomeTaxNio += other.CardIncomeTaxNio;
            ProductRefundsNio += other.ProductRefundsNio;
            ExchangeIncomeNio += other.ExchangeIncomeNio;
            ProductSalesGrossProfitNio += other.ProductSalesGrossProfitNio;
            SaleShippingReconciliationNio += other.SaleShippingReconciliationNio;
            ReturnShippingReconciliationNio += other.ReturnShippingReconciliationNio;
            OriginalShippingRefundNio += other.OriginalShippingRefundNio;
            ReceivedReturnAdjustmentNio += other.ReceivedReturnAdjustmentNio;
            ExchangeOutboundProfitNio += other.ExchangeOutboundProfitNio;
            ExchangeReturnAdjustmentNio += other.ExchangeReturnAdjustmentNio;
            ManualExpensesNio += other.ManualExpensesNio;
            LoanInterestNio += other.LoanInterestNio;
            LostInventoryIssuesNio += other.LostInventoryIssuesNio;
            PurchaseShortageLossNio += other.PurchaseShortageLossNio;
            SupplierRefundsNio += other.SupplierRefundsNio;
        }
    }

    private static void WriteMonthlySummaryRow(ClosedXML.Excel.IXLWorksheet worksheet, int row, int year, int month, MonthlyTotalsAccumulator? totals)
    {
        var values = totals ?? new MonthlyTotalsAccumulator();
        worksheet.Cell(row, 1).Value = year;
        worksheet.Cell(row, 2).Value = new[]
        {
            "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
        }[month - 1];
        WriteMonthlySummaryValues(worksheet, row, values);
    }

    private static void WriteMonthlySummaryTotalRow(
        ClosedXML.Excel.IXLWorksheet worksheet,
        int row,
        string label,
        IEnumerable<MonthlyTotalsAccumulator> totals)
    {
        var total = new MonthlyTotalsAccumulator();
        foreach (var item in totals)
            total.Add(item);

        worksheet.Cell(row, 2).Value = label;
        worksheet.Range(row, 1, row, 24).Style.Font.Bold = true;
        worksheet.Range(row, 1, row, 24).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
        WriteMonthlySummaryValues(worksheet, row, total);
    }

    private static void WriteMonthlySummaryValues(ClosedXML.Excel.IXLWorksheet worksheet, int row, MonthlyTotalsAccumulator totals)
    {
        var values = new decimal[]
        {
            totals.SalesNio, totals.CardCommissionNio, totals.CardIncomeTaxNio, totals.ProductRefundsNio,
            totals.ExchangeIncomeNio, totals.IncomeNio, totals.ProductSalesGrossProfitNio,
            totals.SaleShippingReconciliationNio, totals.ReturnShippingReconciliationNio,
            totals.OriginalShippingRefundNio, totals.ReceivedReturnAdjustmentNio,
            totals.ExchangeOutboundProfitNio, totals.ExchangeReturnAdjustmentNio, totals.GrossProfitNio,
            totals.ManualExpensesNio, totals.LoanInterestNio, totals.LostInventoryIssuesNio,
            totals.PurchaseShortageLossNio, totals.SupplierRefundsNio, totals.ExpensesNio,
            totals.GrossProfitNio - totals.ExpensesNio,
            totals.IncomeNio == 0 ? 0 : Math.Round((totals.GrossProfitNio - totals.ExpensesNio) / totals.IncomeNio * 100, 2)
        };

        for (var column = 0; column < values.Length; column++)
            worksheet.Cell(row, column + 3).Value = values[column];
    }
}
