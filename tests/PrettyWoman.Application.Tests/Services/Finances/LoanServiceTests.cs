using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Loans;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Finances;

public class LoanServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesLoanAndIncomeMovement()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var date = new DateTime(2026, 6, 22, 9, 0, 0, DateTimeKind.Utc);

        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            CreatedAt = date,
            LoanOwnerId = 1,
            InitialAmount = 3650m,
            Comments = "Capital temporal"
        });

        Assert.Equal(3650m, loan.InitialAmount);
        Assert.Equal(100m, loan.InitialAmountUsd);
        Assert.Equal(3650m, loan.Balance);
        Assert.Equal(36.5m, loan.ExchangeRate);
        Assert.True(loan.IsActive);
        Assert.Equal("Capital temporal", loan.Comments);

        var movement = await context.FinancialMovements.SingleAsync();
        Assert.Equal((int)MovementDirectionOptions.In, movement.MovementDirectionId);
        Assert.Equal((int)FinancialMovementTypeOption.LoanReceived, movement.FinancialMovementTypeId);
        Assert.Equal(loan.Id, movement.LoanId);
        Assert.Equal(3650m, movement.Amount);
    }

    [Fact]
    public async Task PayAsync_CreatesOutcomeMovementAndUpdatesBalance()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });

        var updatedLoan = await service.PayAsync(loan.Id, new PayLoanDTO
        {
            Amount = 400m,
            Comments = "Primer abono"
        });

        Assert.Equal(600m, updatedLoan.Balance);
        Assert.Null(updatedLoan.ClosedAt);
        Assert.True(updatedLoan.IsActive);
        var payment = Assert.Single(updatedLoan.Payments);
        Assert.Equal(400m, payment.Amount);
        Assert.Equal("Primer abono", payment.Comments);

        var paymentMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanPayment);
        Assert.Equal((int)MovementDirectionOptions.Out, paymentMovement.MovementDirectionId);
        Assert.Equal(loan.Id, paymentMovement.LoanId);
        Assert.Equal(400m, paymentMovement.Amount);
    }

    [Fact]
    public async Task PayAsync_ClosesLoanWhenBalanceIsFullyPaid()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var paymentDate = new DateTime(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });

        var updatedLoan = await service.PayAsync(loan.Id, new PayLoanDTO
        {
            CreatedAt = paymentDate,
            Amount = 1000m
        });

        Assert.Equal(0m, updatedLoan.Balance);
        Assert.Equal(paymentDate, updatedLoan.ClosedAt);
        Assert.False(updatedLoan.IsActive);
    }

    [Fact]
    public async Task PayAsync_RejectsAmountGreaterThanBalance()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.PayAsync(loan.Id, new PayLoanDTO
        {
            Amount = 1000.01m
        }));

        Assert.Equal("El pago no puede ser mayor que el saldo pendiente del prestamo.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesLoanWhenItHasNoPayments()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });

        var updatedLoan = await service.UpdateAsync(loan.Id, new UpdateLoanDTO
        {
            LoanOwnerId = 2,
            InitialAmount = 730m,
            Comments = "Correccion"
        });

        Assert.Equal(2, updatedLoan.LoanOwnerId);
        Assert.Equal("Banco", updatedLoan.LoanOwnerName);
        Assert.Equal(730m, updatedLoan.InitialAmount);
        Assert.Equal(20m, updatedLoan.InitialAmountUsd);
        Assert.Equal(730m, updatedLoan.Balance);
        Assert.Equal("Correccion", updatedLoan.Comments);

        var receivedMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanReceived);
        Assert.Equal(730m, receivedMovement.Amount);
    }

    [Fact]
    public async Task UpdateAsync_RejectsLoanWithPayments()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });
        await service.PayAsync(loan.Id, new PayLoanDTO { Amount = 100m });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.UpdateAsync(loan.Id, new UpdateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 900m
        }));

        Assert.Equal("No se puede modificar o eliminar un prestamo que ya tiene pagos.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_RemovesLoanAndReceivedMovementWhenItHasNoPayments()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });

        await service.DeleteAsync(loan.Id);

        Assert.False(await context.Loans.AnyAsync(storedLoan => storedLoan.Id == loan.Id));
        Assert.False(await context.FinancialMovements.AnyAsync(movement => movement.LoanId == loan.Id));
    }

    [Fact]
    public async Task DeleteAsync_RejectsLoanWithPayments()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });
        await service.PayAsync(loan.Id, new PayLoanDTO { Amount = 100m });

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.DeleteAsync(loan.Id));

        Assert.Equal("No se puede modificar o eliminar un prestamo que ya tiene pagos.", exception.Message);
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext context)
    {
        context.MovementDirections.AddRange(
            new MovementDirection { Id = (int)MovementDirectionOptions.In, Name = "In" },
            new MovementDirection { Id = (int)MovementDirectionOptions.Out, Name = "Out" });
        context.FinancialMovementTypes.AddRange(
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.LoanReceived, Name = "LoanReceived" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.LoanPayment, Name = "LoanPayment" });
        context.LoanOwners.AddRange(
            new LoanOwner { Id = 1, Name = "Duenio", Enabled = true },
            new LoanOwner { Id = 2, Name = "Banco", Enabled = true });
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
