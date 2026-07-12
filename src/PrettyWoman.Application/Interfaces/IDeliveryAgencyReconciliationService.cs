using PrettyWoman.Application.DTOs.DeliveryAgencyReconciliations;

namespace PrettyWoman.Application.Interfaces;

public interface IDeliveryAgencyReconciliationService
{
    Task<int> CreateAsync(CreateDeliveryAgencyReconciliationDTO reconciliation);
}