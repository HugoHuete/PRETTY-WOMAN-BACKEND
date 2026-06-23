using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Loans;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class LoanService(IApplicationDbContext context) : ILoanService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<PaginatedResult<LoanDTO>> GetAllAsync(LoanQueryDTO query)
    {
        NormalizePagination(query);

        var loansQuery = _context.Loans
            .AsNoTracking()
            .AsQueryable();

        if (query.LoanOwnerId.HasValue)
        {
            loansQuery = loansQuery.Where(loan => loan.LoanOwnerId == query.LoanOwnerId.Value);
        }

        if (query.IsActive.HasValue)
        {
            loansQuery = query.IsActive.Value
                ? loansQuery.Where(loan => loan.InitialAmount - loan.LoanPayments.Sum(payment => payment.PrincipalAmount) > 0 && loan.ClosedAt == null)
                : loansQuery.Where(loan => loan.InitialAmount - loan.LoanPayments.Sum(payment => payment.PrincipalAmount) <= 0 || loan.ClosedAt != null);
        }

        var totalCount = await loansQuery.CountAsync();
        var items = await loansQuery
            .OrderByDescending(loan => loan.CreatedAt)
            .ThenByDescending(loan => loan.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(LoanDTOProjection)
            .ToListAsync();

        return new PaginatedResult<LoanDTO>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<LoanDTO> GetByIdAsync(int id)
    {
        return await GetLoanDTOByIdAsync(id);
    }

    public async Task<LoanDTO> CreateAsync(CreateLoanDTO createLoanDTO)
    {
        ValidateLoanAmount(createLoanDTO.InitialAmount);
        await EnsureLoanOwnerExistsAsync(createLoanDTO.LoanOwnerId);

        var exchangeRate = await GetCurrentExchangeRateAsync();
        var createdAt = createLoanDTO.CreatedAt ?? DateTime.UtcNow;
        var comments = createLoanDTO.Comments.NormalizeOptional();
        var loan = new Loan
        {
            CreatedAt = createdAt,
            LoanOwnerId = createLoanDTO.LoanOwnerId,
            InitialAmount = createLoanDTO.InitialAmount,
            InitialAmountUsd = decimal.Round(createLoanDTO.InitialAmount / exchangeRate, 2),
            Comments = comments,
            ExchangeRate = exchangeRate
        };

        await _context.Loans.AddAsync(loan);
        await _context.FinancialMovements.AddAsync(CreateLoanMovement(
            loan,
            null,
            FinancialMovementTypeOption.LoanReceived,
            MovementDirectionOptions.In,
            createLoanDTO.InitialAmount,
            exchangeRate,
            createdAt,
            "Prestamo recibido",
            comments));
        await _context.SaveChangesAsync();

        return await GetLoanDTOByIdAsync(loan.Id);
    }

    public async Task<LoanDTO> UpdateAsync(int id, UpdateLoanDTO updateLoanDTO)
    {
        var loan = await _context.Loans
            .Include(loan => loan.LoanPayments)
            .Include(loan => loan.FinancialMovements)
            .FirstOrDefaultAsync(loan => loan.Id == id)
            ?? throw new AppNotFoundException($"El prestamo con id '{id}' no existe.");

        EnsureLoanHasNoPayments(loan);
        ValidateLoanAmount(updateLoanDTO.InitialAmount);
        await EnsureLoanOwnerExistsAsync(updateLoanDTO.LoanOwnerId);

        var comments = updateLoanDTO.Comments.NormalizeOptional();
        loan.CreatedAt = updateLoanDTO.CreatedAt ?? loan.CreatedAt;
        loan.LoanOwnerId = updateLoanDTO.LoanOwnerId;
        loan.InitialAmount = updateLoanDTO.InitialAmount;
        loan.InitialAmountUsd = decimal.Round(updateLoanDTO.InitialAmount / loan.ExchangeRate, 2);
        loan.ClosedAt = null;
        loan.Comments = comments;

        var receivedMovement = GetLoanReceivedMovement(loan);
        receivedMovement.CreatedAt = loan.CreatedAt;
        receivedMovement.Amount = updateLoanDTO.InitialAmount;
        receivedMovement.Comments = comments;

        await _context.SaveChangesAsync();

        return await GetLoanDTOByIdAsync(loan.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var loan = await _context.Loans
            .Include(loan => loan.LoanPayments)
            .Include(loan => loan.FinancialMovements)
            .FirstOrDefaultAsync(loan => loan.Id == id)
            ?? throw new AppNotFoundException($"El prestamo con id '{id}' no existe.");

        EnsureLoanHasNoPayments(loan);
        _context.FinancialMovements.RemoveRange(loan.FinancialMovements);
        _context.Loans.Remove(loan);

        await _context.SaveChangesAsync();
    }

    public async Task<LoanDTO> PayAsync(int id, PayLoanDTO payLoanDTO)
    {
        var loan = await _context.Loans
            .Include(loan => loan.LoanPayments)
            .FirstOrDefaultAsync(loan => loan.Id == id)
            ?? throw new AppNotFoundException($"El prestamo con id '{id}' no existe.");

        var balance = CalculateBalance(loan);
        if (loan.ClosedAt.HasValue || balance <= 0)
        {
            throw new AppBadRequestException("El prestamo ya esta pagado.");
        }

        ValidatePaymentAmount(payLoanDTO.Amount, balance);
        ValidateInterestAmount(payLoanDTO.InterestAmount);

        var paymentDate = payLoanDTO.CreatedAt ?? DateTime.UtcNow;
        var comments = payLoanDTO.Comments.NormalizeOptional();
        var payment = new LoanPayment
        {
            Loan = loan,
            CreatedAt = paymentDate,
            PrincipalAmount = payLoanDTO.Amount,
            InterestAmount = payLoanDTO.InterestAmount,
            ExchangeRate = loan.ExchangeRate,
            Comments = comments
        };

        await _context.LoanPayments.AddAsync(payment);
        await _context.FinancialMovements.AddAsync(CreateLoanMovement(
            loan,
            payment,
            FinancialMovementTypeOption.LoanPayment,
            MovementDirectionOptions.Out,
            payLoanDTO.Amount,
            loan.ExchangeRate,
            paymentDate,
            "Pago de prestamo",
            comments));

        if (payLoanDTO.InterestAmount > 0)
        {
            await _context.FinancialMovements.AddAsync(CreateLoanMovement(
                loan,
                payment,
                FinancialMovementTypeOption.LoanInterest,
                MovementDirectionOptions.Out,
                payLoanDTO.InterestAmount,
                loan.ExchangeRate,
                paymentDate,
                "Interes de prestamo",
                comments));
        }

        UpdateClosedAt(loan, balance - payLoanDTO.Amount);
        await _context.SaveChangesAsync();

        return await GetLoanDTOByIdAsync(loan.Id);
    }

    public async Task<LoanDTO> UpdatePaymentAsync(int id, int paymentId, UpdateLoanPaymentDTO updatePaymentDTO)
    {
        var payment = await _context.LoanPayments
            .Include(payment => payment.Loan)
                .ThenInclude(loan => loan!.LoanPayments)
            .Include(payment => payment.FinancialMovements)
            .FirstOrDefaultAsync(payment => payment.Id == paymentId && payment.LoanId == id)
            ?? throw new AppNotFoundException($"El pago de prestamo con id '{paymentId}' no existe.");

        var loan = payment.Loan
            ?? throw new AppNotFoundException($"El prestamo con id '{id}' no existe.");

        var balance = CalculateBalance(loan);
        var availableBalance = balance + payment.PrincipalAmount;
        ValidatePaymentAmount(updatePaymentDTO.Amount, availableBalance);
        ValidateInterestAmount(updatePaymentDTO.InterestAmount);

        var paymentDate = updatePaymentDTO.CreatedAt ?? payment.CreatedAt;
        var comments = updatePaymentDTO.Comments.NormalizeOptional();

        payment.CreatedAt = paymentDate;
        payment.PrincipalAmount = updatePaymentDTO.Amount;
        payment.InterestAmount = updatePaymentDTO.InterestAmount;
        payment.Comments = comments;

        var principalMovement = GetPaymentMovement(payment, FinancialMovementTypeOption.LoanPayment);
        principalMovement.CreatedAt = paymentDate;
        principalMovement.Amount = updatePaymentDTO.Amount;
        principalMovement.Comments = comments;

        var interestMovement = payment.FinancialMovements
            .FirstOrDefault(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanInterest);

        if (updatePaymentDTO.InterestAmount > 0)
        {
            if (interestMovement is null)
            {
                await _context.FinancialMovements.AddAsync(CreateLoanMovement(
                    loan,
                    payment,
                    FinancialMovementTypeOption.LoanInterest,
                    MovementDirectionOptions.Out,
                    updatePaymentDTO.InterestAmount,
                    loan.ExchangeRate,
                    paymentDate,
                    "Interes de prestamo",
                    comments));
            }
            else
            {
                interestMovement.CreatedAt = paymentDate;
                interestMovement.Amount = updatePaymentDTO.InterestAmount;
                interestMovement.Comments = comments;
            }
        }
        else if (interestMovement is not null)
        {
            _context.FinancialMovements.Remove(interestMovement);
        }

        var duplicatedInterestMovements = payment.FinancialMovements
            .Where(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanInterest && movement != interestMovement)
            .ToList();
        if (duplicatedInterestMovements.Count > 0)
        {
            _context.FinancialMovements.RemoveRange(duplicatedInterestMovements);
        }

        UpdateClosedAt(loan, availableBalance - updatePaymentDTO.Amount);
        await _context.SaveChangesAsync();

        return await GetLoanDTOByIdAsync(loan.Id);
    }

    private async Task<LoanDTO> GetLoanDTOByIdAsync(int id)
    {
        return await _context.Loans
            .AsNoTracking()
            .Where(loan => loan.Id == id)
            .Select(LoanDTOProjection)
            .FirstOrDefaultAsync()
            ?? throw new AppNotFoundException($"El prestamo con id '{id}' no existe.");
    }

    private static readonly Expression<Func<Loan, LoanDTO>> LoanDTOProjection = loan => new LoanDTO
    {
        Id = loan.Id,
        CreatedAt = loan.CreatedAt,
        LoanOwnerId = loan.LoanOwnerId,
        LoanOwnerName = loan.LoanOwner != null ? loan.LoanOwner.Name : null,
        InitialAmount = loan.InitialAmount,
        InitialAmountUsd = loan.InitialAmountUsd,
        Balance = loan.InitialAmount - loan.LoanPayments.Sum(payment => payment.PrincipalAmount),
        InterestPaidAmount = loan.LoanPayments.Sum(payment => payment.InterestAmount),
        ClosedAt = loan.ClosedAt,
        Comments = loan.Comments,
        ExchangeRate = loan.ExchangeRate,
        IsActive = loan.InitialAmount - loan.LoanPayments.Sum(payment => payment.PrincipalAmount) > 0 && loan.ClosedAt == null,
        Payments = loan.LoanPayments
            .OrderByDescending(payment => payment.CreatedAt)
            .ThenByDescending(payment => payment.Id)
            .Select(payment => new LoanPaymentDTO
            {
                Id = payment.Id,
                CreatedAt = payment.CreatedAt,
                Amount = payment.PrincipalAmount,
                InterestAmount = payment.InterestAmount,
                TotalAmount = payment.PrincipalAmount + payment.InterestAmount,
                ExchangeRate = payment.ExchangeRate,
                Comments = payment.Comments
            })
            .ToList()
    };

    private static FinancialMovement CreateLoanMovement(
        Loan loan,
        LoanPayment? loanPayment,
        FinancialMovementTypeOption movementType,
        MovementDirectionOptions direction,
        decimal amount,
        decimal exchangeRate,
        DateTime createdAt,
        string description,
        string? comments)
    {
        return new FinancialMovement
        {
            Description = description,
            CreatedAt = createdAt,
            MovementDirectionId = (int)direction,
            FinancialMovementTypeId = (int)movementType,
            Loan = loan,
            LoanPayment = loanPayment,
            Amount = amount,
            ExchangeRate = exchangeRate,
            Comments = comments
        };
    }

    private async Task EnsureLoanOwnerExistsAsync(int loanOwnerId)
    {
        var exists = await _context.LoanOwners
            .AnyAsync(loanOwner => loanOwner.Id == loanOwnerId && loanOwner.Enabled);

        if (!exists)
        {
            throw new AppBadRequestException($"El responsable de prestamo con id '{loanOwnerId}' no existe o no esta habilitado.");
        }
    }

    private static void EnsureLoanHasNoPayments(Loan loan)
    {
        if (loan.LoanPayments.Count > 0)
        {
            throw new AppBadRequestException("No se puede modificar o eliminar un prestamo que ya tiene pagos.");
        }
    }

    private static FinancialMovement GetLoanReceivedMovement(Loan loan)
    {
        return loan.FinancialMovements
            .FirstOrDefault(movement => movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.LoanReceived)
            ?? throw new AppBadRequestException("El prestamo no tiene movimiento financiero de ingreso asociado.");
    }

    private static FinancialMovement GetPaymentMovement(LoanPayment payment, FinancialMovementTypeOption movementType)
    {
        return payment.FinancialMovements
            .FirstOrDefault(movement => movement.FinancialMovementTypeId == (int)movementType)
            ?? throw new AppBadRequestException("El pago de prestamo no tiene movimiento financiero asociado.");
    }

    private static decimal CalculateBalance(Loan loan)
    {
        return loan.InitialAmount - loan.LoanPayments.Sum(payment => payment.PrincipalAmount);
    }

    private static void UpdateClosedAt(Loan loan, decimal balance)
    {
        if (balance > 0)
        {
            loan.ClosedAt = null;
            return;
        }

        loan.ClosedAt = loan.LoanPayments
                     .OrderByDescending(payment => payment.CreatedAt)
                     .FirstOrDefault()?.CreatedAt;
    }

    private static void ValidateLoanAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new AppBadRequestException("El monto inicial del prestamo debe ser mayor que cero.");
        }
    }

    private static void ValidatePaymentAmount(decimal amount, decimal balance)
    {
        if (amount <= 0)
        {
            throw new AppBadRequestException("El monto del pago debe ser mayor que cero.");
        }

        if (amount > balance)
        {
            throw new AppBadRequestException("El pago no puede ser mayor que el saldo pendiente del prestamo.");
        }
    }

    private static void ValidateInterestAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new AppBadRequestException("El monto de interes no puede ser negativo.");
        }
    }

    private static void NormalizePagination(LoanQueryDTO query)
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
    }

    private async Task<decimal> GetCurrentExchangeRateAsync()
    {
        var exchangeRate = await _context.DollarExchangeRates
            .Where(rate => rate.Enabled)
            .OrderByDescending(rate => rate.StartDate)
            .Select(rate => (decimal?)rate.BankRate)
            .FirstOrDefaultAsync();

        return exchangeRate
            ?? throw new AppBadRequestException("Debe existir una tasa de cambio bancaria habilitada para registrar prestamos.");
    }
}
