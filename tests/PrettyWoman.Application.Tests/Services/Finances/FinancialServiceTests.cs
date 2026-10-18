using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PrettyWoman.Application.DTOs.Finances;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Finances;

public class FinancialServiceTests
{
    [Fact]
    public async Task ExportMonthlySummaryAsync_ReturnsDetailedMonthlyAndAnnualTotals()
    {
        await using var context = CreateContext();
        var juneDate = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var julyDate = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        var reconciliation = new DeliveryAgencyReconciliation
        {
            Id = 1,
            DeliveryAgencyId = 1,
            ReconciliationDate = juneDate,
            SettlementExchangeRate = 36.5m
        };
        context.Sales.Add(new Sale
        {
            Id = 1,
            SaleDate = juneDate,
            SaleChannelId = 1,
            UserId = "user",
            Total = 1000m
        });
        context.SaleProducts.Add(new SaleProduct
        {
            Id = 1,
            SaleId = 1,
            ProductId = 1,
            Quantity = 1,
            GrossProfit = 500m
        });
        context.SalePaymentMovements.Add(new SalePaymentMovement
        {
            Id = 1,
            SaleId = 1,
            MovementDate = juneDate,
            PaymentMethodId = 1,
            GrossAmount = 1000m,
            CommissionAmount = 20m,
            IncomeTaxAmount = 10m,
            UserId = "user"
        });
        context.SaleDeliveries.Add(new SaleDelivery
        {
            Id = 1,
            Code = "DEL-1",
            SaleId = 1,
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            DeliveryStatusId = (int)DeliveryStatusCode.Completed,
            UserId = "user",
            ShippingChargedToClient = 100m,
            ShippingPaidToAgency = 80m,
            DeliveryAgencyReconciliation = reconciliation
        });
        context.SaleReturns.Add(new SaleReturn
        {
            Id = 1,
            OriginalSaleId = 1,
            StatusId = (int)SaleReturnStatusOption.Completed,
            ProductRefundTotal = 100m,
            OriginalShippingRefund = 20m,
            ReturnShippingChargedToClient = 10m,
            ReturnShippingPaidToAgency = 15m,
            RefundTotal = 110m,
            RefundedAt = juneDate,
            DeliveryAgencyReconciliation = reconciliation
        });
        context.SaleReturnItems.Add(new SaleReturnItem
        {
            Id = 1,
            SaleReturnId = 1,
            OriginalSaleProductId = 1,
            ProductId = 1,
            Quantity = 1,
            RecognizedUnitAmount = 30m,
            OriginalUnitCost = 40m,
            ReceivedAt = juneDate
        });
        context.ProductInventoryIssues.Add(new ProductInventoryIssue
        {
            Id = 1,
            ProductId = 1,
            ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Missing,
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.ConfirmedLost,
            Quantity = 2,
            UnitCostNioAtIssue = 15m,
            IssueDate = juneDate,
            ResolvedAt = juneDate
        });
        context.LoanPayments.Add(new LoanPayment
        {
            Id = 1,
            LoanId = 1,
            PaymentDate = juneDate,
            PrincipalAmount = 75m,
            InterestAmount = 25m
        });
        context.PurchaseShortages.Add(new PurchaseShortage
        {
            Id = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 1,
            LossAmountNio = 80m,
            ShortageDate = juneDate
        });
        context.FinancialMovements.Add(CreateMovement(
            "Gasto operativo",
            juneDate,
            MovementDirectionOptions.Out,
            FinancialMovementTypeOption.Expense,
            50m));
        context.FinancialMovements.Add(CreateMovement(
            "Gasto de julio",
            julyDate,
            MovementDirectionOptions.Out,
            FinancialMovementTypeOption.Expense,
            50m));
        context.SupplierRefunds.Add(new SupplierRefund
        {
            Id = 1,
            OrderId = 1,
            FinancialMovementId = 3,
            AmountNio = 30m,
            RefundedAt = julyDate
        });
        await context.SaveChangesAsync();

        var service = new FinancialService(context);

        var file = await service.ExportMonthlySummaryAsync(2026, 2026);

        using var workbook = new XLWorkbook(new MemoryStream(file));
        var worksheet = workbook.Worksheet("Resumen financiero");
        var headers = worksheet.Row(2).CellsUsed()
            .ToDictionary(cell => cell.GetString(), cell => cell.Address.ColumnNumber);
        var juneRow = worksheet.RowsUsed().Single(row => row.Cell(2).GetString() == "junio").RowNumber();

        Assert.Equal(1000m, worksheet.Cell(juneRow, headers["Ventas brutas"]).GetValue<decimal>());
        Assert.Equal(-20m, worksheet.Cell(juneRow, headers["Comisiones de tarjeta"]).GetValue<decimal>());
        Assert.Equal(-10m, worksheet.Cell(juneRow, headers["Impuestos de tarjeta"]).GetValue<decimal>());
        Assert.Equal(25m, worksheet.Cell(juneRow, headers["Intereses de préstamos"]).GetValue<decimal>());
        Assert.Equal(80m, worksheet.Cell(juneRow, headers["Faltantes de compras"]).GetValue<decimal>());
        Assert.Equal(-100m, worksheet.Cell(juneRow, headers["Devoluciones de productos"]).GetValue<decimal>());
        Assert.Equal(20m, worksheet.Cell(juneRow, headers["Conciliación de envíos"]).GetValue<decimal>());
        Assert.Equal(-5m, worksheet.Cell(juneRow, headers["Conciliación de devoluciones"]).GetValue<decimal>());
        Assert.Equal(-20m, worksheet.Cell(juneRow, headers["Reembolso de envío original"]).GetValue<decimal>());
        Assert.Equal(10m, worksheet.Cell(juneRow, headers["Ajuste por devolución recibida"]).GetValue<decimal>());
        Assert.Equal(30m, worksheet.Cell(juneRow, headers["Incidencias perdidas"]).GetValue<decimal>());
        Assert.Equal(50m, worksheet.Cell(juneRow, headers["Gastos manuales"]).GetValue<decimal>());
        Assert.Equal(870m, worksheet.Cell(juneRow, headers["Ingresos netos"]).GetValue<decimal>());
        Assert.Equal(475m, worksheet.Cell(juneRow, headers["Ganancia bruta"]).GetValue<decimal>());
        Assert.Equal(185m, worksheet.Cell(juneRow, headers["Gastos totales"]).GetValue<decimal>());
        Assert.Equal(290m, worksheet.Cell(juneRow, headers["Utilidad"]).GetValue<decimal>());

        var julyRow = worksheet.RowsUsed().Single(row => row.Cell(2).GetString() == "julio").RowNumber();
        Assert.Equal(-30m, worksheet.Cell(julyRow, headers["Reembolsos de proveedor"]).GetValue<decimal>());
        Assert.Equal(20m, worksheet.Cell(julyRow, headers["Gastos totales"]).GetValue<decimal>());

        var annualTotalRow = worksheet.RowsUsed().Single(row => row.Cell(2).GetString() == "2026 Total").RowNumber();
        Assert.Equal(870m, worksheet.Cell(annualTotalRow, headers["Ingresos netos"]).GetValue<decimal>());
        Assert.Equal(270m, worksheet.Cell(annualTotalRow, headers["Utilidad"]).GetValue<decimal>());
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_IncludesNetShippingMarginWithoutMixingItIntoProductIncome()
    {
        await using var context = CreateContext();
        var reconciliationDate = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var refundedAt = new DateTime(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);
        var reconciliation = new DeliveryAgencyReconciliation
        {
            Id = 1,
            DeliveryAgencyId = 1,
            ReconciliationDate = reconciliationDate,
            SettlementExchangeRate = 36.5m
        };
        var sale = new Sale
        {
            Id = 1,
            SaleDate = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
            SaleChannelId = 1,
            UserId = "user",
            Total = 900m
        };
        var saleReturn = new SaleReturn
        {
            Id = 1,
            OriginalSaleId = sale.Id,
            StatusId = (int)SaleReturnStatusOption.Completed,
            ProductRefundTotal = 100m,
            OriginalShippingRefund = 20m,
            ReturnShippingChargedToClient = 10m,
            ReturnShippingPaidToAgency = 15m,
            RefundTotal = 110m,
            RefundedAt = refundedAt,
            DeliveryAgencyReconciliation = reconciliation
        };
        context.Sales.Add(sale);
        context.SaleProducts.Add(new SaleProduct
        {
            Id = 1,
            SaleId = sale.Id,
            ProductId = 1,
            Quantity = 1,
            GrossProfit = 400m
        });
        context.SaleDeliveries.Add(new SaleDelivery
        {
            Id = 1,
            Code = "DEL-1",
            SaleId = sale.Id,
            MunicipalityId = 1,
            DeliveryAgencyId = 1,
            DeliveryStatusId = (int)DeliveryStatusCode.Completed,
            UserId = "user",
            ShippingChargedToClient = 100m,
            ShippingPaidToAgency = 80m,
            DeliveryAgencyReconciliation = reconciliation
        });
        context.SaleReturns.Add(saleReturn);
        context.FinancialMovements.AddRange(
            CreateMovement("Remesa", reconciliationDate, MovementDirectionOptions.In, FinancialMovementTypeOption.DeliveryAgencyReconciliation, 900m),
            CreateMovement("Liquidacion agencia", reconciliationDate, MovementDirectionOptions.Out, FinancialMovementTypeOption.DeliveryAgencyReconciliation, 95m));
        await context.SaveChangesAsync();

        var service = new FinancialService(context);

        var result = await service.GetMonthlySummaryAsync(2026, 2026);

        var june = result.Years.Single().Months.Single(month => month.Month == 6);
        Assert.Equal(800m, june.IncomeNio);
        Assert.Equal(395m, june.GrossProfitNio);
        Assert.Equal(0m, june.ExpensesNio);
        Assert.Equal(395m, june.UtilityNio);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_AccountsForPaymentFeesLoanInterestAndPurchaseShortages()
    {
        await using var context = CreateContext();
        var juneDate = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var julyDate = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        context.Sales.Add(new Sale
        {
            Id = 1,
            SaleDate = juneDate,
            SaleChannelId = 1,
            UserId = "user",
            Total = 1000m
        });
        context.SaleProducts.Add(new SaleProduct
        {
            Id = 1,
            SaleId = 1,
            ProductId = 1,
            Quantity = 1,
            GrossProfit = 500m
        });
        context.SalePaymentMovements.AddRange(
            new SalePaymentMovement
            {
                Id = 1,
                SaleId = 1,
                MovementDate = juneDate,
                PaymentMethodId = 1,
                GrossAmount = 1000m,
                CommissionAmount = 20m,
                IncomeTaxAmount = 10m,
                UserId = "user"
            },
            new SalePaymentMovement
            {
                Id = 2,
                SaleId = 1,
                MovementDate = juneDate,
                MovementDirectionId = (int)MovementDirectionOptions.Out,
                PaymentMethodId = 1,
                GrossAmount = 1000m,
                CommissionAmount = 20m,
                IncomeTaxAmount = 10m,
                UserId = "user"
            });
        context.LoanPayments.Add(new LoanPayment
        {
            Id = 1,
            LoanId = 1,
            PaymentDate = juneDate,
            PrincipalAmount = 75m,
            InterestAmount = 25m
        });
        context.PurchaseShortages.Add(new PurchaseShortage
        {
            Id = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 1,
            LossAmountNio = 80m,
            ShortageDate = juneDate
        });
        context.FinancialMovements.Add(CreateMovement(
            "Otro gasto de julio",
            julyDate,
            MovementDirectionOptions.Out,
            FinancialMovementTypeOption.Expense,
            50m));
        context.SupplierRefunds.Add(new SupplierRefund
        {
            Id = 1,
            OrderId = 1,
            FinancialMovementId = 2,
            AmountNio = 30m,
            RefundedAt = julyDate
        });
        context.FinancialMovements.Add(CreateMovement(
            "Reembolso proveedor",
            julyDate,
            MovementDirectionOptions.In,
            FinancialMovementTypeOption.SupplierRefund,
            30m));
        await context.SaveChangesAsync();

        var service = new FinancialService(context);

        var result = await service.GetMonthlySummaryAsync(2026, 2026);

        var year = Assert.Single(result.Years);
        var june = year.Months.Single(month => month.Month == 6);
        var july = year.Months.Single(month => month.Month == 7);
        Assert.Equal(970m, june.IncomeNio);
        Assert.Equal(470m, june.GrossProfitNio);
        Assert.Equal(105m, june.ExpensesNio);
        Assert.Equal(365m, june.UtilityNio);
        Assert.Equal(20m, july.ExpensesNio);
        Assert.Equal(-20m, july.UtilityNio);
        Assert.Equal(970m, year.IncomeNio);
        Assert.Equal(470m, year.GrossProfitNio);
        Assert.Equal(125m, year.ExpensesNio);
        Assert.Equal(345m, year.UtilityNio);
    }

    [Fact]
    public async Task GetCurrentBalanceAsync_ReturnsZeroWhenThereAreNoMovements()
    {
        await using var context = CreateContext();
        var service = new FinancialService(context);

        var balance = await service.GetCurrentBalanceAsync();

        Assert.Equal(0m, balance.IncomeTotalNio);
        Assert.Equal(0m, balance.ExpenseTotalNio);
        Assert.Equal(0m, balance.BalanceNio);
        Assert.Equal(0, balance.MovementCount);
        Assert.Null(balance.LastMovementAt);
    }

    [Fact]
    public async Task GetCurrentBalanceAsync_SumsIncomeAndSubtractsExpenses()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var firstDate = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
        var lastDate = new DateTime(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc);

        context.FinancialMovements.AddRange(
            CreateMovement("Venta #1", firstDate, MovementDirectionOptions.In, FinancialMovementTypeOption.SalePayment, 1500m),
            CreateMovement("Venta #2", lastDate, MovementDirectionOptions.In, FinancialMovementTypeOption.SalePayment, 700m),
            CreateMovement("Compra proveedor", firstDate, MovementDirectionOptions.Out, FinancialMovementTypeOption.SupplierPayment, 900m));
        await context.SaveChangesAsync();

        var service = new FinancialService(context);

        var balance = await service.GetCurrentBalanceAsync();

        Assert.Equal(2200m, balance.IncomeTotalNio);
        Assert.Equal(900m, balance.ExpenseTotalNio);
        Assert.Equal(1300m, balance.BalanceNio);
        Assert.Equal(3, balance.MovementCount);
        Assert.Equal(lastDate, balance.LastMovementAt);
    }

    [Fact]
    public async Task GetMovementTypesAsync_ReturnsMovementTypesOrderedById()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var service = new FinancialService(context);

        var movementTypes = (await service.GetMovementTypesAsync()).ToList();

        Assert.Contains(movementTypes, type => type.Id == (int)FinancialMovementTypeOption.OwnerInvestment && type.Name == "OwnerInvestment");
        Assert.Contains(movementTypes, type => type.Id == (int)FinancialMovementTypeOption.Expense && type.Name == "Expense");
        Assert.Equal(movementTypes.OrderBy(type => type.Id).Select(type => type.Id), movementTypes.Select(type => type.Id));
    }
    [Fact]
    public async Task GetMovementsAsync_ReturnsPagedFilteredMovements()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);

        context.FinancialMovements.AddRange(
            CreateMovement("Venta anterior", new DateTime(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.In, FinancialMovementTypeOption.SalePayment, 500m),
            CreateMovement("Gasto operativo", new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.Out, FinancialMovementTypeOption.Expense, 120m),
            CreateMovement("Compra proveedor", new DateTime(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.Out, FinancialMovementTypeOption.SupplierPayment, 900m),
            CreateMovement("Venta posterior", new DateTime(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.In, FinancialMovementTypeOption.SalePayment, 1500m));
        await context.SaveChangesAsync();

        var service = new FinancialService(context);

        var result = await service.GetMovementsAsync(new FinancialMovementQueryDTO
        {
            Page = 1,
            PageSize = 1,
            FromDate = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc),
            MovementDirectionId = (int)MovementDirectionOptions.Out
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal("Compra proveedor", item.Description);
        Assert.Equal((int)MovementDirectionOptions.Out, item.MovementDirectionId);
        Assert.Equal("Out", item.MovementDirectionName);
        Assert.Equal((int)FinancialMovementTypeOption.SupplierPayment, item.FinancialMovementTypeId);
        Assert.Equal("SupplierPayment", item.FinancialMovementTypeName);
        Assert.Equal(900m, item.Amount);
    }

    [Fact]
    public async Task GetMovementsAsync_FiltersByMovementType()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);

        context.FinancialMovements.AddRange(
            CreateMovement("Venta", new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.In, FinancialMovementTypeOption.SalePayment, 500m),
            CreateMovement("Gasto", new DateTime(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.Out, FinancialMovementTypeOption.Expense, 100m));
        await context.SaveChangesAsync();

        var service = new FinancialService(context);

        var result = await service.GetMovementsAsync(new FinancialMovementQueryDTO
        {
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.Expense
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("Gasto", item.Description);
        Assert.Equal((int)FinancialMovementTypeOption.Expense, item.FinancialMovementTypeId);
    }

    [Fact]
    public async Task CreateManualMovementAsync_CreatesOwnerInvestmentAsIncome()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var service = new FinancialService(context);
        var date = new DateTime(2026, 6, 22, 9, 0, 0, DateTimeKind.Utc);

        var movement = await service.CreateManualMovementAsync(new CreateFinancialMovementDTO
        {
            Description = "Aporte inicial",
            MovementDate = date,
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.OwnerInvestment,
            Amount = 1000m,
            Comments = "Capital"
        });

        Assert.Equal("Aporte inicial", movement.Description);
        Assert.Equal(date, movement.MovementDate);
        Assert.Equal((int)MovementDirectionOptions.In, movement.MovementDirectionId);
        Assert.Equal((int)FinancialMovementTypeOption.OwnerInvestment, movement.FinancialMovementTypeId);
        Assert.Equal(1000m, movement.Amount);
        Assert.Equal(36.5m, movement.ExchangeRate);
        Assert.Equal("Capital", movement.Comments);
        Assert.Null(movement.ExpenseCategoryId);
    }

    [Fact]
    public async Task CreateManualMovementAsync_CreatesExpenseAsOutcomeWithExpenseCategory()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var service = new FinancialService(context);

        var movement = await service.CreateManualMovementAsync(new CreateFinancialMovementDTO
        {
            Description = "Pago de luz",
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.Expense,
            ExpenseCategoryId = 1,
            Amount = 350m
        });

        Assert.Equal((int)MovementDirectionOptions.Out, movement.MovementDirectionId);
        Assert.Equal((int)FinancialMovementTypeOption.Expense, movement.FinancialMovementTypeId);
        Assert.Equal(1, movement.ExpenseCategoryId);
        Assert.Equal(350m, movement.Amount);
    }

    [Fact]
    public async Task CreateManualMovementAsync_RejectsNonManualMovementTypes()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var service = new FinancialService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateManualMovementAsync(new CreateFinancialMovementDTO
        {
            Description = "Venta manual",
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.SalePayment,
            Amount = 500m
        }));

        Assert.Equal("Este tipo de movimiento financiero no se puede registrar manualmente desde finanzas.", exception.Message);
    }

    [Fact]
    public async Task CreateManualMovementAsync_RequiresDirectionForAdjustments()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var service = new FinancialService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateManualMovementAsync(new CreateFinancialMovementDTO
        {
            Description = "Ajuste",
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.Adjustment,
            Amount = 100m
        }));

        Assert.Equal("Los ajustes deben indicar una direccion de movimiento valida.", exception.Message);
    }

    [Fact]
    public async Task GetMovementsAsync_ThrowsWhenDateRangeIsInvalid()
    {
        await using var context = CreateContext();
        var service = new FinancialService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.GetMovementsAsync(new FinancialMovementQueryDTO
        {
            FromDate = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc)
        }));

        Assert.Equal("La fecha inicial no puede ser mayor que la fecha final.", exception.Message);
    }

    [Fact]
    public async Task UpdateManualMovementAsync_UpdatesManualMovementAndPreservesExchangeRate()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var movement = CreateMovement("Aporte", new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.In, FinancialMovementTypeOption.OwnerInvestment, 1000m);
        movement.ExchangeRate = 35m;
        context.FinancialMovements.Add(movement);
        await context.SaveChangesAsync();
        var service = new FinancialService(context);
        var newDate = new DateTime(2026, 6, 22, 8, 0, 0, DateTimeKind.Utc);

        var updated = await service.UpdateManualMovementAsync(movement.Id, new UpdateFinancialMovementDTO
        {
            Description = "Retiro socio",
            MovementDate = newDate,
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.OwnerWithdrawal,
            Amount = 250m,
            Comments = "Correccion"
        });

        Assert.Equal("Retiro socio", updated.Description);
        Assert.Equal(newDate, updated.MovementDate);
        Assert.Equal((int)MovementDirectionOptions.Out, updated.MovementDirectionId);
        Assert.Equal((int)FinancialMovementTypeOption.OwnerWithdrawal, updated.FinancialMovementTypeId);
        Assert.Equal(250m, updated.Amount);
        Assert.Equal(35m, updated.ExchangeRate);
        Assert.Equal("Correccion", updated.Comments);
        Assert.Null(updated.ExpenseCategoryId);
    }

    [Fact]
    public async Task UpdateManualMovementAsync_RejectsNonManualMovement()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var movement = CreateMovement("Venta", new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.In, FinancialMovementTypeOption.SalePayment, 500m);
        context.FinancialMovements.Add(movement);
        await context.SaveChangesAsync();
        var service = new FinancialService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.UpdateManualMovementAsync(movement.Id, new UpdateFinancialMovementDTO
        {
            Description = "Ajuste venta",
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.Adjustment,
            MovementDirectionId = (int)MovementDirectionOptions.In,
            Amount = 500m
        }));

        Assert.Equal("Este tipo de movimiento financiero no se puede registrar manualmente desde finanzas.", exception.Message);
    }

    [Fact]
    public async Task DeleteManualMovementAsync_RemovesManualMovement()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var movement = CreateMovement("Ajuste caja", new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.In, FinancialMovementTypeOption.Adjustment, 100m);
        context.FinancialMovements.Add(movement);
        await context.SaveChangesAsync();
        var service = new FinancialService(context);

        await service.DeleteManualMovementAsync(movement.Id);

        Assert.False(await context.FinancialMovements.AnyAsync(storedMovement => storedMovement.Id == movement.Id));
    }

    [Fact]
    public async Task DeleteManualMovementAsync_RejectsLinkedMovement()
    {
        await using var context = CreateContext();
        await SeedFinanceCatalogAsync(context);
        var movement = CreateMovement("Ajuste relacionado", new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc), MovementDirectionOptions.In, FinancialMovementTypeOption.Adjustment, 100m);
        movement.OrderId = 99;
        context.FinancialMovements.Add(movement);
        await context.SaveChangesAsync();
        var service = new FinancialService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.DeleteManualMovementAsync(movement.Id));

        Assert.Equal("Este movimiento financiero esta relacionado con otro flujo y no se puede modificar manualmente desde finanzas.", exception.Message);
    }
    private static FinancialMovement CreateMovement(
        string description,
        DateTime createdAt,
        MovementDirectionOptions direction,
        FinancialMovementTypeOption type,
        decimal amount)
    {
        return new FinancialMovement
        {
            Description = description,
            MovementDate = createdAt,
            CreatedAt = createdAt,
            MovementDirectionId = (int)direction,
            FinancialMovementTypeId = (int)type,
            Amount = amount,
            ExchangeRate = 36.5m
        };
    }

    private static async Task SeedFinanceCatalogAsync(ApplicationDbContext context)
    {
        context.MovementDirections.AddRange(
            new MovementDirection { Id = (int)MovementDirectionOptions.In, Name = "In" },
            new MovementDirection { Id = (int)MovementDirectionOptions.Out, Name = "Out" });
        context.FinancialMovementTypes.AddRange(
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.OwnerInvestment, Name = "OwnerInvestment" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SupplierPayment, Name = "SupplierPayment" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SalePayment, Name = "SalePayment" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.Expense, Name = "Expense" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.OwnerWithdrawal, Name = "OwnerWithdrawal" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.Adjustment, Name = "Adjustment" });
        context.ExpenseCategories.Add(new ExpenseCategory { Id = 1, Name = "Servicios", Enabled = true });
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
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }
}



