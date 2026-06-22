using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Finances;

namespace PrettyWoman.Application.Interfaces;

public interface IFinancialService
{
    Task<CurrentFinancialBalanceDTO> GetCurrentBalanceAsync();
    Task<IEnumerable<FinancialMovementTypeDTO>> GetMovementTypesAsync();
    Task<PaginatedResult<FinancialMovementDTO>> GetMovementsAsync(FinancialMovementQueryDTO query);
    Task<FinancialMovementDTO> CreateManualMovementAsync(CreateFinancialMovementDTO createMovementDTO);
    Task<FinancialMovementDTO> UpdateManualMovementAsync(int id, UpdateFinancialMovementDTO updateMovementDTO);
    Task DeleteManualMovementAsync(int id);
}
