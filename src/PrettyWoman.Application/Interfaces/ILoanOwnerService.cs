using PrettyWoman.Application.DTOs.LoanOwners;

namespace PrettyWoman.Application.Interfaces;

public interface ILoanOwnerService
{
    Task<LoanOwnerDTO> GetByIdAsync(int id);
    Task<IEnumerable<LoanOwnerDTO>> GetAllAsync();
    Task<int> CreateAsync(CreateLoanOwnerDTO createLoanOwnerDTO);
    Task UpdateAsync(int id, UpdateLoanOwnerDTO updateLoanOwnerDTO);
}
