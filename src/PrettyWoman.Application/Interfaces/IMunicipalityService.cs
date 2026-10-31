using PrettyWoman.Application.DTOs.Municipalities;

namespace PrettyWoman.Application.Interfaces;

public interface IMunicipalityService
{
    Task<IEnumerable<MunicipalityDTO>> GetAllAsync(int? departmentId = null);
}
