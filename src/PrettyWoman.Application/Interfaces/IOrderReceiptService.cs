using PrettyWoman.Application.DTOs.Orders;

namespace PrettyWoman.Application.Interfaces;

public interface IOrderReceiptService
{
    Task<OrderReceiptDTO> ReceiveAsync(int orderId, ReceiveOrderDTO receiveOrderDTO);
    Task<OrderReceiptDTO> UpdateShippingCostAsync(int orderId, int receiptId, UpdateOrderReceiptDTO updateOrderReceiptDTO);
}
