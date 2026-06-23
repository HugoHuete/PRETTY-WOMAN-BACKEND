using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Loans;

namespace PrettyWoman.Application.Interfaces;

public interface ILoanService
{
    Task<PaginatedResult<LoanDTO>> GetAllAsync(LoanQueryDTO query);
    Task<LoanDTO> GetByIdAsync(int id);
    Task<LoanDTO> CreateAsync(CreateLoanDTO createLoanDTO);
    Task<LoanDTO> UpdateAsync(int id, UpdateLoanDTO updateLoanDTO);
    Task DeleteAsync(int id);
    Task<LoanDTO> PayAsync(int id, PayLoanDTO payLoanDTO);
}
