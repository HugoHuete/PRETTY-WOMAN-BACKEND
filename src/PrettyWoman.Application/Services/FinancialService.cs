using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Finances;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
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

    public async Task<PaginatedResult<FinancialMovementDTO>> GetMovementsAsync(FinancialMovementQueryDTO query)
    {
        NormalizePagination(query);

        var movementsQuery = _context.FinancialMovements
            .AsNoTracking()
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
            .Select(movement => new FinancialMovementDTO
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
            })
            .ToListAsync();

        return new PaginatedResult<FinancialMovementDTO>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    private static void NormalizePagination(FinancialMovementQueryDTO query)
    {
        if (query.Page <= 0)
        {
            throw new AppBadRequestException("La p�gina debe ser mayor que cero.");
        }

        if (query.PageSize <= 0)
        {
            throw new AppBadRequestException("El tama�o de p�gina debe ser mayor que cero.");
        }

        if (query.PageSize > 100)
        {
            throw new AppBadRequestException("El tama�o de p�gina no puede ser mayor que 100.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
        {
            throw new AppBadRequestException("La fecha inicial no puede ser mayor que la fecha final.");
        }
    }
}
