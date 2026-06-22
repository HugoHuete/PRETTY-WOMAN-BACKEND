using PrettyWoman.Application.DTOs.Orders;

namespace PrettyWoman.Application.Interfaces;

public interface IOrderReceiptService
{
    Task<OrderReceiptDTO> ReceiveAsync(int orderId, ReceiveOrderDTO receiveOrderDTO);
}
