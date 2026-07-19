using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.InventoryAdjustments;

namespace PrettyWoman.Application.Interfaces;

public interface IInventoryAdjustmentService
{
    Task<PaginatedResult<InventoryAdjustmentDTO>> GetAllAsync(InventoryAdjustmentQueryDTO query);
    Task<InventoryAdjustmentDTO> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateInventoryAdjustmentDTO request);
}
