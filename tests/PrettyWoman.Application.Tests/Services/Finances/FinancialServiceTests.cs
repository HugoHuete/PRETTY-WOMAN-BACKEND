using Microsoft.EntityFrameworkCore;
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
            ToDate = new DateTime(2026, 6, 21, 23, 59, 59, DateTimeKind.Utc),
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
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SupplierPayment, Name = "SupplierPayment" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SalePayment, Name = "SalePayment" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.Expense, Name = "Expense" });
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
