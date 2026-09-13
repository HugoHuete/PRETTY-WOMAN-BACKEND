using PrettyWoman.Application.DTOs.ShippingCompanies;

namespace PrettyWoman.Application.Interfaces;

public interface IShippingCompanyService
{
    Task<IEnumerable<ShippingCompanyDTO>> GetAllAsync();
}
