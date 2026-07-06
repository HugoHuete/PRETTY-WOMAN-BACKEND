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
            LoanDate = date,
            LoanOwnerId = 1,
            InitialAmount = 3650m,
            Comments = "Capital temporal"
        });

        Assert.Equal(3650m, loan.InitialAmount);
        Assert.Equal(100m, loan.InitialAmountUsd);
        Assert.Equal(3650m, loan.Balance);
        Assert.Equal(0m, loan.InterestPaidAmount);
        Assert.Equal(36.5m, loan.ExchangeRate);
        Assert.True(loan.IsActive);
        Assert.Equal("Capital temporal", loan.Comments);
        Assert.False(await context.LoanPayments.AnyAsync());

        var movement = await context.FinancialMovements.SingleAsync();
        Assert.Equal((int)MovementDirectionOptions.In, movement.MovementDirectionId);
        Assert.Equal((int)FinancialMovementTypeOption.LoanReceived, movement.FinancialMovementTypeId);
        Assert.Equal(loan.Id, movement.LoanId);
        Assert.Null(movement.LoanPaymentId);
        Assert.Equal(3650m, movement.Amount);
    }

    [Fact]
    public async Task PayAsync_CreatesLoanPaymentAndOutcomeMovements()
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
            InterestAmount = 50m,
            Comments = "Primer abono"
        });

        Assert.Equal(600m, updatedLoan.Balance);
        Assert.Equal(50m, updatedLoan.InterestPaidAmount);
        Assert.Null(updatedLoan.ClosedAt);
        Assert.True(updatedLoan.IsActive);
        var payment = Assert.Single(updatedLoan.Payments);
        Assert.Equal(400m, payment.Amount);
        Assert.Equal(50m, payment.InterestAmount);
        Assert.Equal(450m, payment.TotalAmount);
        Assert.Equal("Primer abono", payment.Comments);

        var storedPayment = await context.LoanPayments.SingleAsync();
        Assert.Equal(payment.Id, storedPayment.Id);
        Assert.Equal(loan.Id, storedPayment.LoanId);
        Assert.Equal(400m, storedPayment.PrincipalAmount);
        Assert.Equal(50m, storedPayment.InterestAmount);

        var paymentMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanPayment);
        Assert.Equal((int)MovementDirectionOptions.Out, paymentMovement.MovementDirectionId);
        Assert.Equal(loan.Id, paymentMovement.LoanId);
        Assert.Equal(payment.Id, paymentMovement.LoanPaymentId);
        Assert.Equal(400m, paymentMovement.Amount);

        var interestMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanInterest);
        Assert.Equal((int)MovementDirectionOptions.Out, interestMovement.MovementDirectionId);
        Assert.Equal(loan.Id, interestMovement.LoanId);
        Assert.Equal(payment.Id, interestMovement.LoanPaymentId);
        Assert.Equal(50m, interestMovement.Amount);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsInterestPaidAmountFromLoanPayments()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });
        await service.PayAsync(loan.Id, new PayLoanDTO { Amount = 200m, InterestAmount = 25m });
        await service.PayAsync(loan.Id, new PayLoanDTO { Amount = 300m, InterestAmount = 40m });

        var result = await service.GetAllAsync(new LoanQueryDTO());

        var item = Assert.Single(result.Items);
        Assert.Equal(500m, item.Balance);
        Assert.Equal(65m, item.InterestPaidAmount);
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
            PaymentDate = paymentDate,
            Amount = 1000m,
            InterestAmount = 20m
        });

        Assert.Equal(0m, updatedLoan.Balance);
        Assert.Equal(20m, updatedLoan.InterestPaidAmount);
        Assert.Equal(paymentDate, updatedLoan.ClosedAt);
        Assert.False(updatedLoan.IsActive);
    }

    [Fact]
    public async Task PayAsync_RejectsAmountGreaterThanCalculatedBalance()
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
    public async Task UpdatePaymentAsync_UpdatesLoanPaymentMovementsAndCalculatedBalance()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var firstDate = new DateTime(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc);
        var secondDate = new DateTime(2026, 6, 23, 10, 0, 0, DateTimeKind.Utc);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });
        var paidLoan = await service.PayAsync(loan.Id, new PayLoanDTO
        {
            PaymentDate = firstDate,
            Amount = 400m,
            InterestAmount = 50m,
            Comments = "Primer pago"
        });
        var paymentId = paidLoan.Payments.Single().Id;

        var updatedLoan = await service.UpdatePaymentAsync(loan.Id, paymentId, new UpdateLoanPaymentDTO
        {
            PaymentDate = secondDate,
            Amount = 250m,
            InterestAmount = 30m,
            Comments = "Correccion pago"
        });

        Assert.Equal(750m, updatedLoan.Balance);
        Assert.Equal(30m, updatedLoan.InterestPaidAmount);
        Assert.Null(updatedLoan.ClosedAt);
        var payment = Assert.Single(updatedLoan.Payments);
        Assert.Equal(paymentId, payment.Id);
        Assert.Equal(secondDate, payment.PaymentDate);
        Assert.Equal(250m, payment.Amount);
        Assert.Equal(30m, payment.InterestAmount);
        Assert.Equal(280m, payment.TotalAmount);
        Assert.Equal("Correccion pago", payment.Comments);

        var storedPayment = await context.LoanPayments.SingleAsync();
        Assert.Equal(250m, storedPayment.PrincipalAmount);
        Assert.Equal(30m, storedPayment.InterestAmount);

        var paymentMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanPayment);
        Assert.Equal(paymentId, paymentMovement.LoanPaymentId);
        Assert.Equal(250m, paymentMovement.Amount);
        Assert.Equal(secondDate, paymentMovement.MovementDate);

        var interestMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanInterest);
        Assert.Equal(paymentId, interestMovement.LoanPaymentId);
        Assert.Equal(30m, interestMovement.Amount);
        Assert.Equal(secondDate, interestMovement.MovementDate);
    }

    [Fact]
    public async Task UpdatePaymentAsync_RemovesInterestMovementWhenInterestIsZero()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });
        var paidLoan = await service.PayAsync(loan.Id, new PayLoanDTO
        {
            Amount = 400m,
            InterestAmount = 50m
        });
        var paymentId = paidLoan.Payments.Single().Id;

        var updatedLoan = await service.UpdatePaymentAsync(loan.Id, paymentId, new UpdateLoanPaymentDTO
        {
            Amount = 400m,
            InterestAmount = 0m
        });

        Assert.Equal(0m, updatedLoan.InterestPaidAmount);
        Assert.Equal(0m, (await context.LoanPayments.SingleAsync()).InterestAmount);
        Assert.False(await context.FinancialMovements.AnyAsync(movement =>
            movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanInterest));
    }

    [Fact]
    public async Task UpdatePaymentAsync_CreatesInterestMovementWhenPaymentOriginallyHadNoInterest()
    {
        await using var context = CreateContext();
        await SeedCatalogAsync(context);
        var service = new LoanService(context);
        var paymentDate = new DateTime(2026, 6, 23, 10, 0, 0, DateTimeKind.Utc);
        var loan = await service.CreateAsync(new CreateLoanDTO
        {
            LoanOwnerId = 1,
            InitialAmount = 1000m
        });
        var paidLoan = await service.PayAsync(loan.Id, new PayLoanDTO
        {
            Amount = 400m,
            InterestAmount = 0m
        });
        var paymentId = paidLoan.Payments.Single().Id;

        var updatedLoan = await service.UpdatePaymentAsync(loan.Id, paymentId, new UpdateLoanPaymentDTO
        {
            PaymentDate = paymentDate,
            Amount = 400m,
            InterestAmount = 25m,
            Comments = "Agrega interes"
        });

        Assert.Equal(25m, updatedLoan.InterestPaidAmount);

        var interestMovement = await context.FinancialMovements
            .SingleAsync(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanInterest);
        Assert.Equal(paymentId, interestMovement.LoanPaymentId);
        Assert.Equal(25m, interestMovement.Amount);
        Assert.Equal(paymentDate, interestMovement.MovementDate);
        Assert.Equal("Agrega interes", interestMovement.Comments);
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
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.LoanPayment, Name = "LoanPayment" },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.LoanInterest, Name = "LoanInterest" });
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
