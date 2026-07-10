using PrettyWoman.Application.DTOs.Sales;

namespace PrettyWoman.Application.Interfaces;

public interface ISaleService
{
    Task<IEnumerable<SaleDTO>> GetAllAsync();
    Task<SaleDTO> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateSaleDTO createSaleDTO);
    Task PatchHeaderAsync(int id, PatchSaleHeaderDTO patchSaleHeaderDTO);
    Task ReplaceProductsAsync(int id, ReplaceSaleProductsDTO replaceSaleProductsDTO);
    Task<int> AddPaymentMovementAsync(int saleId, CreateSalePaymentMovementDTO createPaymentMovementDTO);
    Task UpdatePaymentMovementAsync(int saleId, int paymentMovementId, UpdateSalePaymentMovementDTO updatePaymentMovementDTO);
    Task<int> RefundPaymentMovementAsync(int saleId, int paymentMovementId, RefundSalePaymentMovementDTO refundPaymentMovementDTO);
}


