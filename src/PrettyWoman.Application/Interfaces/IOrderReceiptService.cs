using PrettyWoman.Application.DTOs.Orders;

namespace PrettyWoman.Application.Interfaces;

public interface IOrderReceiptService
{
    Task<ICollection<OrderReceiptSummaryDTO>> GetAllAsync(int orderId);
    Task<OrderReceiptDTO> GetByIdAsync(int orderId, int receiptId);
    Task<OrderReceiptDTO> ReceiveAsync(int orderId, ReceiveOrderDTO receiveOrderDTO);
    Task<OrderReceiptDTO> UpdateShippingCostAsync(int orderId, int receiptId, UpdateOrderReceiptDTO updateOrderReceiptDTO);
}
