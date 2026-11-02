using PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;

namespace PrettyWoman.Application.Interfaces;

public interface IDeliveryAgencyReconciliationService
{
    Task<IEnumerable<PendingReconciliationDeliveryDTO>> GetPendingDeliveriesAsync(int? deliveryAgencyId = null);
    Task<IEnumerable<PendingReconciliationReturnDTO>> GetPendingReturnsAsync(int? deliveryAgencyId = null);
    Task<int> CreateAsync(CreateDeliveryAgencyReconciliationDTO reconciliation);
}
