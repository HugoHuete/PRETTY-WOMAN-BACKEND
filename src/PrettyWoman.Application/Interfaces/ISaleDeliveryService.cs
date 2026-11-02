using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Interfaces;

public interface ISaleDeliveryService
{
    Task<PaginatedResult<SaleDeliveryListItemDTO>> GetAllAsync(SaleDeliveryQueryDTO query);
    Task<int> CreateAsync(int saleId, CreateSaleDeliveryDTO delivery);
    Task PatchAsync(int saleId, int deliveryId, PatchSaleDeliveryDTO delivery);
    Task MarkAsSentAsync(int saleId, int deliveryId);
    Task MarkAsDeliveredPendingSelectionAsync(int saleId, int deliveryId);
    Task MarkAsCompletedAsync(int saleId, int deliveryId);
    Task MarkAsFailedAsync(int saleId, int deliveryId);
    Task CancelAsync(int saleId, int deliveryId);
    Task SyncActiveAmountToCollectAsync(int saleId, decimal saleTotal, IEnumerable<SalePaymentMovement> paymentMovements);
    Task EnsureSaleChannelCanBeChangedAsync(int saleId, int saleChannelId);
}
