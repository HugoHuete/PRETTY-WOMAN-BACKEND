using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Municipalities;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Application.Services;

public class MunicipalityService(IApplicationDbContext context) : IMunicipalityService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<IEnumerable<MunicipalityDTO>> GetAllAsync(int? departmentId = null)
    {
        var municipalities = _context.Municipalities.AsNoTracking();

        if (departmentId.HasValue)
            municipalities = municipalities.Where(municipality => municipality.DepartmentId == departmentId.Value);

        return await municipalities
            .OrderBy(municipality => municipality.Name)
            .Select(municipality => new MunicipalityDTO
            {
                Id = municipality.Id,
                Name = municipality.Name,
                DepartmentId = municipality.DepartmentId,
                DepartmentName = municipality.Department!.Name
            })
            .ToListAsync();
    }
}
