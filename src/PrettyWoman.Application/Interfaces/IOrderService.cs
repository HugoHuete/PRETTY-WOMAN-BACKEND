using PrettyWoman.Application.DTOs.Orders;

namespace PrettyWoman.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDTO> GetByIdAsync(int id);
    Task<IEnumerable<OrderDTO>> GetAllAsync();
    Task<IEnumerable<OrderTrackingNumberDTO>> GetTrackingNumbersAsync(int orderId);
    Task<int> CreateAsync(CreateOrderDTO createOrderDTO);
    Task UpdateAsync(int id, UpdateOrderDTO updateOrderDTO);
    Task<OrderDTO> CloseShortagesAsync(int id, CloseOrderShortagesDTO closeShortagesDTO);
    Task<OrderDTO> CreateSupplierRefundAsync(int id, CreateSupplierRefundDTO createSupplierRefundDTO);
    Task<IEnumerable<OrderTrackingNumberDTO>> AddTrackingNumbersAsync(int orderId, IEnumerable<CreateOrderTrackingNumberDTO> createTrackingDTOs);
    Task<OrderTrackingNumberDTO> UpdateTrackingNumberAsync(int orderId, int trackingId, UpdateOrderTrackingNumberDTO updateTrackingDTO);
    Task DeleteTrackingNumberAsync(int orderId, int trackingId);
}
