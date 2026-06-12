using PrettyWoman.Application.DTOs.Sizes;

namespace PrettyWoman.Application.Interfaces;

public interface ISizeService
{
    Task<SizeDTO> GetByIdAsync(int id);
    Task<IEnumerable<SizeDTO>> GetAllAsync();
    Task<int> CreateAsync(CreateSizeDTO createSizeDTO);
    Task UpdateAsync(int id, UpdateSizeDTO updateSizeDTO);
}
