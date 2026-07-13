using PrettyWoman.Application.DTOs.Sales;

namespace PrettyWoman.Application.Interfaces;

public interface ISaleReturnService
{
    Task<IEnumerable<SaleReturnDTO>> GetBySaleIdAsync(int saleId);
    Task<int> CreateAsync(int saleId, CreateSaleReturnDTO request);
    Task RegisterAgencyPickupAsync(int saleId, int returnId, ProcessSaleReturnDTO request);
    Task ReceiveAsync(int saleId, int returnId, ReceiveSaleReturnDTO request);
    Task CancelAsync(int saleId, int returnId);
}
