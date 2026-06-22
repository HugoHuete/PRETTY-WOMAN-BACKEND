using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Finances;

namespace PrettyWoman.Application.Interfaces;

public interface IFinancialService
{
    Task<CurrentFinancialBalanceDTO> GetCurrentBalanceAsync();
    Task<PaginatedResult<FinancialMovementDTO>> GetMovementsAsync(FinancialMovementQueryDTO query);
}
