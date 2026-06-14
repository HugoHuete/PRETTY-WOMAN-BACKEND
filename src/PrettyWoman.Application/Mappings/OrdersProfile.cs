using AutoMapper;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Mappings;

public class OrdersProfile : Profile
{
    public OrdersProfile()
    {
        CreateMap<CreateOrderDTO, Order>();
        CreateMap<UpdateOrderDTO, Order>();
        CreateMap<Order, OrderDTO>()
            .ForMember(
                destination => destination.OrderStatusName,
                options => options.MapFrom(source => source.OrderStatus != null ? source.OrderStatus.Name : null))
            .ForMember(
                destination => destination.SupplierName,
                options => options.MapFrom(source => source.Supplier != null ? source.Supplier.Name : null));

        CreateMap<CreateOrderTrackingNumberDTO, OrderTrackingNumber>();
        CreateMap<UpdateOrderTrackingNumberDTO, OrderTrackingNumber>();
        CreateMap<OrderTrackingNumber, OrderTrackingNumberDTO>()
            .ForMember(
                destination => destination.ShippingCompanyName,
                options => options.MapFrom(source => source.ShippingCompany != null ? source.ShippingCompany.Name : null));
    }
}
