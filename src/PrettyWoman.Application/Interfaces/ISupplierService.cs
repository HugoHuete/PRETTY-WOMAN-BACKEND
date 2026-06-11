using PrettyWoman.Application.DTOs.Suppliers;

namespace PrettyWoman.Application.Interfaces;

public interface ISupplierService
{
    Task<int> CreateAsync(CreateSupplierDTO createSupplierDTO);
}