using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Interfaces;

public interface ISalePaymentMovementService
{
    Task<List<SalePaymentMovement>> CreateInitialAsync(List<CreateSalePaymentMovementDTO> paymentMovements);
    Task CreateFinancialMovementsAsync(IEnumerable<SalePaymentMovement> paymentMovements);
    Task<int> AddAsync(int saleId, CreateSalePaymentMovementDTO paymentMovement);
    Task PatchAsync(int saleId, int paymentMovementId, UpdateSalePaymentMovementDTO paymentMovement);
    Task<int> RefundAsync(int saleId, int paymentMovementId, RefundSalePaymentMovementDTO refund);
}
