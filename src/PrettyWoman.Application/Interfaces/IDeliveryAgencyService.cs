using PrettyWoman.Application.DTOs.DeliveryAgencies;

namespace PrettyWoman.Application.Interfaces;

public interface IDeliveryAgencyService
{
    Task<DeliveryAgencyDTO> GetByIdAsync(int id);
    Task<IEnumerable<DeliveryAgencyDTO>> GetAllAsync();
    Task<int> CreateAsync(CreateDeliveryAgencyDTO createDeliveryAgencyDTO);
    Task UpdateAsync(int id, UpdateDeliveryAgencyDTO updateDeliveryAgencyDTO);
}
