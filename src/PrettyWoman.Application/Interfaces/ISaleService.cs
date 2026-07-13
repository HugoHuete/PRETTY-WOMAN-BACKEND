using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Sales;

namespace PrettyWoman.Application.Interfaces;

public interface ISaleService
{
    Task<PaginatedResult<SaleDTO>> GetAllAsync(SaleQueryDTO query);
    Task<SaleDTO> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateSaleDTO createSaleDTO);
    Task PatchHeaderAsync(int id, PatchSaleHeaderDTO patchSaleHeaderDTO);
    Task ReplaceProductsAsync(int id, ReplaceSaleProductsDTO replaceSaleProductsDTO);
    Task AddSelectionHoldsAsync(int saleId, List<CreateSaleSelectionProductDTO> selectionProducts);
    Task ResolveSelectionHoldAsync(int saleId, int holdId, ResolveSelectionHoldDTO resolution);
    Task MarkSelectionHoldAsReturnedAsync(int saleId, int holdId);
    Task<int> AddPaymentMovementAsync(int saleId, CreateSalePaymentMovementDTO paymentMovement);
    Task UpdatePaymentMovementAsync(int saleId, int paymentMovementId, UpdateSalePaymentMovementDTO paymentMovement);
    Task<int> RefundPaymentMovementAsync(int saleId, int paymentMovementId, RefundSalePaymentMovementDTO refund);
    Task<int> CreateDeliveryAsync(int saleId, CreateSaleDeliveryDTO delivery);
    Task UpdateDeliveryAsync(int saleId, int deliveryId, PatchSaleDeliveryDTO delivery);
    Task MarkDeliveryAsSentAsync(int saleId, int deliveryId);
    Task MarkDeliveryAsDeliveredPendingSelectionAsync(int saleId, int deliveryId);
    Task MarkDeliveryAsCompletedAsync(int saleId, int deliveryId);
    Task MarkDeliveryAsFailedAsync(int saleId, int deliveryId);
    Task CancelDeliveryAsync(int saleId, int deliveryId);
    Task CancelAsync(int id);
}
