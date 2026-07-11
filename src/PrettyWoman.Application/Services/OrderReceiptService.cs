using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class OrderReceiptService(IApplicationDbContext context) : IOrderReceiptService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<OrderReceiptDTO> ReceiveAsync(int orderId, ReceiveOrderDTO receiveOrderDTO)
    {
        NormalizeFields(receiveOrderDTO);

        var order = await _context.Orders
            .Include(order => order.Products)
            .Include(order => order.OrderTrackingNumbers)
            .FirstOrDefaultAsync(order => order.Id == orderId)
            ?? throw new AppNotFoundException($"La orden con id '{orderId}' no existe.");

        EnsureOrderCanReceive(order);
        // Check quantities and productIds are valid
        var receivedProducts = ValidateAndGetProducts(order, receiveOrderDTO.Products);
        var receiptDate = receiveOrderDTO.ReceivedDate.NormalizeToUtc() ?? DateTime.UtcNow;

        // Update TrackingNumberStatus (if any) and obtain shipping costs
        var warehouseShippingCostUsd = ApplyTrackingReceipt(order, receiveOrderDTO, receiptDate);
        var warehouseShippingCostNio = Math.Round(warehouseShippingCostUsd * order.ExchangeRate, 2);
        var warehouseShippingAllocations = AllocateWarehouseShipping(receivedProducts, warehouseShippingCostNio);

        var receipt = new ProductReceipt
        {
            OrderId = order.Id,
            ReceivedDate = receiptDate
        };

        foreach (var item in receivedProducts)
        {
            item.Product.ReceivedQuantity += item.Quantity;
            item.Product.AvailableQuantity += item.Quantity;
            item.Product.AllocatedShippingCostNio += warehouseShippingAllocations[item.Product.Id];
            item.Product.TotalCostNio = item.Product.MerchandiseTotalCostNio + item.Product.AllocatedShippingCostNio;
            item.Product.UnitCostNio = Math.Round(item.Product.TotalCostNio / item.Product.Quantity, 6);
            item.Product.UnitCostUsd = order.ExchangeRate == 0
                ? 0
                : Math.Round(item.Product.UnitCostNio / order.ExchangeRate, 2);

            receipt.ProductReceiptDetails.Add(new ProductReceiptDetail
            {
                Product = item.Product,
                Quantity = item.Quantity
            });

            await _context.InventoryMovements.AddAsync(new InventoryMovement
            {
                MovementDate = receiptDate,
                Product = item.Product,
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.PurchaseReceived,
                FromStockBucketId = (int)InventoryStockBucketOption.External,
                ToStockBucketId = (int)InventoryStockBucketOption.Available,
                Quantity = item.Quantity,
                OrderId = order.Id,
                Comments = receiveOrderDTO.Comments
            });
        }

        await _context.ProductReceipts.AddAsync(receipt);

        foreach (var trackingNumber in GetReceivedTrackingNumbers(order, receiveOrderDTO))
        {
            trackingNumber.ProductReceipt = receipt;
        }

        order.WarehouseShippingCostUsd += warehouseShippingCostUsd;
        order.TotalCostNio += warehouseShippingCostNio;
        order.OrderStatusId = ResolveOrderStatus(order);
        order.ReceivedAmountNio = CalculateReceivedAmountNio(order);

        if (warehouseShippingCostNio > 0)
        {
            await _context.FinancialMovements.AddAsync(CreateWarehouseShippingMovement(order, receipt, warehouseShippingCostNio, receiveOrderDTO.Comments, receiptDate));
        }

        await _context.SaveChangesAsync();

        return new OrderReceiptDTO
        {
            Id = receipt.Id,
            OrderId = receipt.OrderId,
            ReceivedDate = receipt.ReceivedDate,
            CreatedAt = receipt.CreatedAt,
            WarehouseShippingCostUsd = warehouseShippingCostUsd,
            WarehouseShippingCostNio = warehouseShippingCostNio,
            OrderStatusId = order.OrderStatusId,
            Products = receivedProducts
                .Select(item => new OrderReceiptProductDTO
                {
                    ProductId = item.Product.Id,
                    Quantity = item.Quantity,
                    AllocatedWarehouseShippingCostNio = warehouseShippingAllocations[item.Product.Id]
                })
                .ToList(),
            TrackingNumberIds = GetReceivedTrackingNumbers(order, receiveOrderDTO)
                .Select(tracking => tracking.Id)
                .ToList()
        };
    }

    private static void EnsureOrderCanReceive(Order order)
    {
        if (order.OrderStatusId == (int)OrderStatusCode.Cancelled)
        {
            throw new AppBadRequestException("No se puede recibir productos de una orden cancelada.");
        }

        if (order.OrderStatusId == (int)OrderStatusCode.Received)
        {
            throw new AppBadRequestException("La orden ya fue recibida completamente.");
        }
    }

    private static List<ReceivedProduct> ValidateAndGetProducts(Order order, ICollection<ReceiveOrderProductDTO> receivedProductDTOs)
    {
        if (receivedProductDTOs.Count == 0)
        {
            throw new AppBadRequestException("Debe enviar al menos un producto recibido.");
        }

        var duplicatedProduct = receivedProductDTOs
            .GroupBy(product => product.ProductId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedProduct != null)
        {
            throw new AppBadRequestException("No puede enviar productos duplicados en la misma recepción.");
        }

        var receivedProducts = new List<ReceivedProduct>();

        foreach (var productDTO in receivedProductDTOs)
        {
            var product = order.Products.FirstOrDefault(product => product.Id == productDTO.ProductId)
                ?? throw new AppBadRequestException($"El producto con id '{productDTO.ProductId}' no pertenece a la orden.");

            var pendingQuantity = product.Quantity - product.ReceivedQuantity;
            if (productDTO.Quantity > pendingQuantity)
            {
                throw new AppBadRequestException($"La cantidad recibida del producto '{product.Id}' supera la cantidad pendiente.");
            }

            receivedProducts.Add(new ReceivedProduct(product, productDTO.Quantity, productDTO.Weight));
        }

        return receivedProducts;
    }

    private static decimal ApplyTrackingReceipt(Order order, ReceiveOrderDTO receiveOrderDTO, DateTime receiptDate)
    {
        var orderHasTrackingNumbers = order.OrderTrackingNumbers.Count != 0;

        // If order has not tracking numbers, the WarehouseShippingCost comes from the request directly.
        if (!orderHasTrackingNumbers)
        {
            if (receiveOrderDTO.TrackingNumbers.Count != 0)
            {
                throw new AppBadRequestException("La orden no tiene trackings registrados; envíe el costo de envío directamente.");
            }

            return receiveOrderDTO.WarehouseShippingCostUsd ?? 0;
        }

        // If order has tracking numnbers, cost must come from tracking numbers not directly in the request.
        if (receiveOrderDTO.WarehouseShippingCostUsd.HasValue && receiveOrderDTO.WarehouseShippingCostUsd.Value > 0)
        {
            throw new AppBadRequestException("Cuando la orden tiene trackings, el costo de envío se registra por tracking.");
        }

        if (receiveOrderDTO.TrackingNumbers.Count == 0)
        {
            throw new AppBadRequestException("Debe enviar al menos un tracking para recepcionar una orden con trackings.");
        }

        var duplicatedTracking = receiveOrderDTO.TrackingNumbers
            .GroupBy(tracking => tracking.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedTracking != null)
        {
            throw new AppBadRequestException("No puede enviar trackings duplicados en la misma recepción.");
        }

        foreach (var trackingDTO in receiveOrderDTO.TrackingNumbers)
        {
            var trackingNumber = order.OrderTrackingNumbers.FirstOrDefault(tracking => tracking.Id == trackingDTO.Id)
                ?? throw new AppBadRequestException($"El tracking con id '{trackingDTO.Id}' no pertenece a la orden.");

            if (trackingNumber.ProductReceiptId.HasValue)
            {
                throw new AppBadRequestException($"El tracking con id '{trackingDTO.Id}' ya fue recepcionado.");
            }

            trackingNumber.Weight = trackingDTO.Weight;
            trackingNumber.ShippingCost = trackingDTO.ShippingCostUsd;
        }

        return receiveOrderDTO.TrackingNumbers.Sum(tracking => tracking.ShippingCostUsd);
    }

    private static List<OrderTrackingNumber> GetReceivedTrackingNumbers(Order order, ReceiveOrderDTO receiveOrderDTO)
    {
        if (receiveOrderDTO.TrackingNumbers.Count == 0)
        {
            return [];
        }

        var trackingIds = receiveOrderDTO.TrackingNumbers.Select(tracking => tracking.Id).ToHashSet();
        return order.OrderTrackingNumbers
            .Where(tracking => trackingIds.Contains(tracking.Id))
            .ToList();
    }

    private static Dictionary<int, decimal> AllocateWarehouseShipping(List<ReceivedProduct> receivedProducts, decimal warehouseShippingCostNio)
    {
        var estimatedWeightByLine = receivedProducts
            .Select(item => item.Weight * item.Quantity)
            .ToList();
        var allocations = AllocateAmount(warehouseShippingCostNio, estimatedWeightByLine);

        return receivedProducts
            .Select((item, index) => new { item.Product.Id, Allocation = allocations[index] })
            .ToDictionary(item => item.Id, item => item.Allocation);
    }

    private static List<decimal> AllocateAmount(decimal total, List<decimal> weights)
    {
        var totalWeight = weights.Sum();
        if (total == 0 || totalWeight == 0)
        {
            return weights.Select(_ => 0m).ToList();
        }

        var allocations = new List<decimal>();
        var assigned = 0m;

        for (var index = 0; index < weights.Count; index++)
        {
            if (index == weights.Count - 1)
            {
                allocations.Add(total - assigned);
                break;
            }

            var allocation = Math.Round(total * weights[index] / totalWeight, 2);
            allocations.Add(allocation);
            assigned += allocation;
        }

        return allocations;
    }
    private static int ResolveOrderStatus(Order order)
    {
        return order.Products.All(product => product.ReceivedQuantity == product.Quantity)
            ? (int)OrderStatusCode.Received
            : (int)OrderStatusCode.PartiallyReceived;
    }

    private static decimal CalculateReceivedAmountNio(Order order)
    {
        if (order.Products.All(product => product.ReceivedQuantity == product.Quantity))
        {
            return order.MerchandiseTotalNio;
        }

        return Math.Round(order.Products.Sum(product =>
            product.Quantity == 0
                ? 0
                : product.MerchandiseTotalCostNio * product.ReceivedQuantity / product.Quantity), 2);
    }

    private static FinancialMovement CreateWarehouseShippingMovement(Order order, ProductReceipt receipt, decimal amountNio, string? comments, DateTime date)
    {
        return new FinancialMovement
        {
            Description = $"Pago de envío bodega a Nicaragua por orden #{order.Id}.",
            MovementDate = date,
            MovementDirectionId = (int)MovementDirectionOptions.Out,
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.WarehouseShippingPayment,
            OrderId = order.Id,
            ProductReceipt = receipt,
            Amount = amountNio,
            ExchangeRate = order.ExchangeRate,
            Comments = comments
        };
    }

    private static void NormalizeFields(ReceiveOrderDTO receiveOrderDTO)
    {
        receiveOrderDTO.Comments = receiveOrderDTO.Comments.NormalizeOptional();
        receiveOrderDTO.TrackingNumbers ??= [];
        receiveOrderDTO.Products ??= [];
    }

    private sealed record ReceivedProduct(Product Product, int Quantity, decimal Weight);
}
