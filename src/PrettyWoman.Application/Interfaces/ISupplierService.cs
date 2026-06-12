using PrettyWoman.Application.DTOs.Suppliers;

namespace PrettyWoman.Application.Interfaces;

public interface ISupplierService
{
    Task<SupplierDTO> GetByIdAsync(int id);
    Task<IEnumerable<SupplierDTO>> GetAllAsync();
    Task<int> CreateAsync(CreateSupplierDTO createSupplierDTO);
}