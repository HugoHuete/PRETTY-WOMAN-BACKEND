using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Clients;

namespace PrettyWoman.Application.Interfaces;

public interface IClientService
{
    Task<ClientDTO> GetByIdAsync(int id);
    Task<PaginatedResult<ClientDTO>> GetAllAsync(ClientQueryDTO query);
    Task<int> CreateAsync(CreateClientDTO createClientDTO);
    Task UpdateAsync(int id, UpdateClientDTO updateClientDTO);
    Task BlockAsync(int id, BlockClientDTO blockClientDTO);
    Task UnblockAsync(int id);
}
