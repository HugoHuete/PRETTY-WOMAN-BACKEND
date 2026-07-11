using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Interfaces;

public interface ISaleDeliveryService
{
    Task<int> CreateAsync(int saleId, CreateSaleDeliveryDTO delivery);
    Task PatchAsync(int saleId, int deliveryId, PatchSaleDeliveryDTO delivery);
    Task MarkAsSentAsync(int saleId, int deliveryId);
    Task SyncActiveAmountToCollectAsync(int saleId, decimal saleTotal, IEnumerable<SalePaymentMovement> paymentMovements);
    Task EnsureSaleChannelCanBeChangedAsync(int saleId, int saleChannelId);
}