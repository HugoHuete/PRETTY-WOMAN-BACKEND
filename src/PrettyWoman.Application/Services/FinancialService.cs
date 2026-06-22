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
                LastMovementAt = group.Max(movement => (DateTime?)movement.CreatedAt)
            })
            .FirstOrDefaultAsync();

        balance ??= new CurrentFinancialBalanceDTO();
        balance.BalanceNio = balance.IncomeTotalNio - balance.ExpenseTotalNio;

        return balance;
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
            movementsQuery = movementsQuery.Where(movement => movement.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            movementsQuery = movementsQuery.Where(movement => movement.CreatedAt < query.ToDate.Value.AddDays(1));
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
            .OrderByDescending(movement => movement.CreatedAt)
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
            CreatedAt = createMovementDTO.CreatedAt ?? DateTime.UtcNow,
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
        movement.CreatedAt = updateMovementDTO.CreatedAt ?? movement.CreatedAt;
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
            CreatedAt = movement.CreatedAt,
            MovementDirectionId = movement.MovementDirectionId,
            MovementDirectionName = movement.MovementDirection != null ? movement.MovementDirection.Name : null,
            FinancialMovementTypeId = movement.FinancialMovementTypeId,
            FinancialMovementTypeName = movement.FinancialMovementType != null ? movement.FinancialMovementType.Name : null,
            ExpenseCategoryId = movement.ExpenseCategoryId,
            OrderId = movement.OrderId,
            SalePaymentId = movement.SalePaymentId,
            LoanId = movement.LoanId,
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

        if (movement.OrderId.HasValue || movement.SalePaymentId.HasValue || movement.LoanId.HasValue)
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
}

