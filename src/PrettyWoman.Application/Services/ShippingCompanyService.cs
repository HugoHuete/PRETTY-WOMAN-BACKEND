using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.ShippingCompanies;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Application.Services;

public class ShippingCompanyService(IApplicationDbContext context) : IShippingCompanyService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<IEnumerable<ShippingCompanyDTO>> GetAllAsync()
    {
        return await _context.ShippingCompanies
            .AsNoTracking()
            .OrderBy(shippingCompany => shippingCompany.Name)
            .Select(shippingCompany => new ShippingCompanyDTO
            {
                Id = shippingCompany.Id,
                Name = shippingCompany.Name,
                Url = shippingCompany.Url
            })
            .ToListAsync();
    }
}
