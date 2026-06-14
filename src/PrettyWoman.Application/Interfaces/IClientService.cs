using PrettyWoman.Application.DTOs.Clients;

namespace PrettyWoman.Application.Interfaces;

public interface IClientService
{
    Task<ClientDTO> GetByIdAsync(int id);
    Task<IEnumerable<ClientDTO>> GetAllAsync();
    Task<int> CreateAsync(CreateClientDTO createClientDTO);
    Task UpdateAsync(int id, UpdateClientDTO updateClientDTO);
    Task BlockAsync(int id, BlockClientDTO blockClientDTO);
    Task UnblockAsync(int id);
}
