using PrettyWoman.Application.DTOs.PaymentTerminals;

namespace PrettyWoman.Application.Interfaces;

public interface IPaymentTerminalService
{
    Task<PaymentTerminalDTO> GetByIdAsync(int id);
    Task<IEnumerable<PaymentTerminalDTO>> GetAllAsync();
    Task<int> CreateAsync(CreatePaymentTerminalDTO createPaymentTerminalDTO);
    Task UpdateAsync(int id, UpdatePaymentTerminalDTO updatePaymentTerminalDTO);
}
