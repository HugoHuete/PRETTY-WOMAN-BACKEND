using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class OrderService(IApplicationDbContext context, IMapper mapper) : IOrderService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateOrderDTO createOrderDTO)
    {
        NormalizeOrderFields(createOrderDTO);
        await EnsureSupplierExistsAsync(createOrderDTO.SupplierId);

        var order = _mapper.Map<Order>(createOrderDTO);
        order.TotalCostNio = createOrderDTO.MerchandiseTotalNio + createOrderDTO.ShippingCostNio;
        order.CreatedAt = DateTime.UtcNow;
        order.OrderStatusId = (int)OrderStatusCode.Pending;

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        return order.Id;
    }

    public async Task UpdateAsync(int id, UpdateOrderDTO updateOrderDTO)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(order => order.Id == id)
            ?? throw new AppNotFoundException($"La orden con id '{id}' no existe.");

        NormalizeOrderFields(updateOrderDTO);
        await EnsureSupplierExistsAsync(updateOrderDTO.SupplierId);

        _mapper.Map(updateOrderDTO, order);
        order.TotalCostNio = updateOrderDTO.MerchandiseTotalNio + updateOrderDTO.ShippingCostNio;

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OrderTrackingNumberDTO>> AddTrackingNumbersAsync(
        int orderId,
        IEnumerable<CreateOrderTrackingNumberDTO> createTrackingDTOs)
    {
        await EnsureOrderExistsAsync(orderId);

        var trackingItems = createTrackingDTOs.ToList();
        if (trackingItems.Count == 0)
        {
            throw new AppBadRequestException("Debe enviar al menos un tracking.");
        }

        foreach (var trackingItem in trackingItems)
        {
            NormalizeTrackingFields(trackingItem);
            await EnsureShippingCompanyExistsAsync(trackingItem.ShippingCompanyId);
            await EnsureProductReceiptExistsAsync(trackingItem.ProductReceiptId);
            await EnsureTrackingNumberIsUniqueAsync(trackingItem.TrackingNumber);
        }

        var duplicatedRequestTracking = trackingItems
            .GroupBy(item => item.TrackingNumber, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedRequestTracking != null)
        {
            throw new AppBadRequestException("No puede enviar números de tracking duplicados en la misma solicitud.");
        }

        var trackingNumbers = trackingItems
            .Select(trackingItem =>
            {
                var trackingNumber = _mapper.Map<OrderTrackingNumber>(trackingItem);
                trackingNumber.OrderId = orderId;
                return trackingNumber;
            })
            .ToList();

        await _context.OrderTrackingNumbers.AddRangeAsync(trackingNumbers);
        await _context.SaveChangesAsync();

        var createdTrackingNumbers = await _context.OrderTrackingNumbers
            .Include(tracking => tracking.ShippingCompany)
            .Where(tracking => trackingNumbers.Select(created => created.Id).Contains(tracking.Id))
            .OrderBy(tracking => tracking.Id)
            .ToListAsync();

        return _mapper.Map<List<OrderTrackingNumberDTO>>(createdTrackingNumbers);
    }

    public async Task<OrderTrackingNumberDTO> UpdateTrackingNumberAsync(
        int orderId,
        int trackingId,
        UpdateOrderTrackingNumberDTO updateTrackingDTO)
    {
        var trackingNumber = await _context.OrderTrackingNumbers
            .Include(tracking => tracking.ShippingCompany)
            .FirstOrDefaultAsync(tracking => tracking.Id == trackingId && tracking.OrderId == orderId)
            ?? throw new AppNotFoundException($"El tracking con id '{trackingId}' no existe para la orden '{orderId}'.");

        NormalizeTrackingFields(updateTrackingDTO);
        await EnsureShippingCompanyExistsAsync(updateTrackingDTO.ShippingCompanyId);
        await EnsureProductReceiptExistsAsync(updateTrackingDTO.ProductReceiptId);
        await EnsureTrackingNumberIsUniqueAsync(updateTrackingDTO.TrackingNumber, trackingId);

        _mapper.Map(updateTrackingDTO, trackingNumber);

        await _context.SaveChangesAsync();

        trackingNumber = await _context.OrderTrackingNumbers
            .Include(tracking => tracking.ShippingCompany)
            .FirstAsync(tracking => tracking.Id == trackingId);

        return _mapper.Map<OrderTrackingNumberDTO>(trackingNumber);
    }

    public async Task DeleteTrackingNumberAsync(int orderId, int trackingId)
    {
        var trackingNumber = await _context.OrderTrackingNumbers
            .FirstOrDefaultAsync(tracking => tracking.Id == trackingId && tracking.OrderId == orderId)
            ?? throw new AppNotFoundException($"El tracking con id '{trackingId}' no existe para la orden '{orderId}'.");

        _context.OrderTrackingNumbers.Remove(trackingNumber);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OrderDTO>> GetAllAsync()
    {
        var orders = await _context.Orders
            .Include(order => order.Supplier)
            .Include(order => order.OrderStatus)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<OrderDTO>>(orders);
    }

    public async Task<OrderDTO> GetByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(order => order.Supplier)
            .Include(order => order.OrderStatus)
            .FirstOrDefaultAsync(order => order.Id == id)
            ?? throw new AppNotFoundException($"La orden con id '{id}' no existe.");

        return _mapper.Map<OrderDTO>(order);
    }

    public async Task<IEnumerable<OrderTrackingNumberDTO>> GetTrackingNumbersAsync(int orderId)
    {
        await EnsureOrderExistsAsync(orderId);

        var trackingNumbers = await _context.OrderTrackingNumbers
            .Include(tracking => tracking.ShippingCompany)
            .Where(tracking => tracking.OrderId == orderId)
            .OrderBy(tracking => tracking.Id)
            .ToListAsync();

        return _mapper.Map<List<OrderTrackingNumberDTO>>(trackingNumbers);
    }

    private async Task EnsureSupplierExistsAsync(int supplierId)
    {
        var exists = await _context.Suppliers.AnyAsync(supplier => supplier.Id == supplierId);

        if (!exists)
        {
            throw new AppNotFoundException($"El proveedor con id '{supplierId}' no existe.");
        }
    }

    private async Task EnsureOrderExistsAsync(int orderId)
    {
        var exists = await _context.Orders.AnyAsync(order => order.Id == orderId);

        if (!exists)
        {
            throw new AppNotFoundException($"La orden con id '{orderId}' no existe.");
        }
    }

    private async Task EnsureOrderStatusExistsAsync(int orderStatusId)
    {
        var exists = await _context.OrderStatuses.AnyAsync(orderStatus => orderStatus.Id == orderStatusId);

        if (!exists)
        {
            throw new AppNotFoundException($"El estado de orden con id '{orderStatusId}' no existe.");
        }
    }

    private async Task EnsureShippingCompanyExistsAsync(int shippingCompanyId)
    {
        var exists = await _context.ShippingCompanies.AnyAsync(shippingCompany => shippingCompany.Id == shippingCompanyId);

        if (!exists)
        {
            throw new AppNotFoundException($"La empresa de envío con id '{shippingCompanyId}' no existe.");
        }
    }

    private async Task EnsureProductReceiptExistsAsync(int? productReceiptId)
    {
        if (!productReceiptId.HasValue)
        {
            return;
        }

        var exists = await _context.ProductReceipts.AnyAsync(productReceipt => productReceipt.Id == productReceiptId.Value);

        if (!exists)
        {
            throw new AppNotFoundException($"La recepción de productos con id '{productReceiptId.Value}' no existe.");
        }
    }

    private async Task EnsureTrackingNumberIsUniqueAsync(string trackingNumber, int? trackingId = null)
    {
        var exists = await _context.OrderTrackingNumbers.AnyAsync(tracking =>
            tracking.Id != trackingId &&
            tracking.TrackingNumber.ToLower() == trackingNumber.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe un tracking con ese número.");
        }
    }

    private static void NormalizeOrderFields(CreateOrderDTO orderDTO)
    {
        orderDTO.Comments = orderDTO.Comments.NormalizeOptional();
    }

    private static void NormalizeTrackingFields(CreateOrderTrackingNumberDTO trackingDTO)
    {
        trackingDTO.TrackingNumber = trackingDTO.TrackingNumber.NormalizeRequired("Número de tracking");
    }
}
