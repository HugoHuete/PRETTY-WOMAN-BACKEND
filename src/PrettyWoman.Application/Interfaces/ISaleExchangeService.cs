using PrettyWoman.Application.DTOs.Sales;

namespace PrettyWoman.Application.Interfaces;

public interface ISaleExchangeService
{
    Task<IEnumerable<SaleExchangeDTO>> GetBySaleIdAsync(int saleId);
    Task<int> CreateAsync(int saleId, CreateSaleExchangeDTO request);
    Task CompleteHandoverAsync(int saleId, int exchangeId);
    Task MarkReturnReceivedAsync(int saleId, int exchangeId, int returnItemId);
    Task CancelAsync(int saleId, int exchangeId);
}
