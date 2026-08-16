using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class OrderReceiptService(
    IApplicationDbContext context,
    IInventoryService inventoryService) : IOrderReceiptService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IInventoryService _inventoryService = inventoryService;

    public async Task<OrderReceiptDTO> ReceiveAsync(int orderId, ReceiveOrderDTO receiveOrderDTO)
    {
        NormalizeFields(receiveOrderDTO);

        var order = await _context.Orders
            .Include(order => order.Products)
            .Include(order => order.OrderTrackingNumbers)
            .FirstOrDefaultAsync(order => order.Id == orderId)
            ?? throw new AppNotFoundException($"La orden con id '{orderId}' no existe.");

        EnsureOrderCanReceive(order, receiveOrderDTO.Products);
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
            ReceivedDate = receiptDate,
            WarehouseShippingCostUsd = warehouseShippingCostUsd,
            WarehouseShippingCostNio = warehouseShippingCostNio
        };

        foreach (var item in receivedProducts)
        {
            var inventoryMovement = _inventoryService.Move(
                item.Product,
                InventoryStockBucketOption.External,
                InventoryStockBucketOption.Available,
                item.Quantity,
                InventoryMovementTypeOption.PurchaseReceived,
                receiptDate
            );
            inventoryMovement.OrderId = order.Id;

            item.Product.AllocatedShippingCostNio += warehouseShippingAllocations[item.Product.Id];
            item.Product.TotalCostNio = item.Product.MerchandiseTotalCostNio + item.Product.AllocatedShippingCostNio;
            item.Product.UnitCostNio = CalculateUnitCostNio(item.Product);
            item.Product.UnitCostUsd = order.ExchangeRate == 0
                ? 0
                : Math.Round(item.Product.UnitCostNio / order.ExchangeRate, 2);

            receipt.ProductReceiptDetails.Add(new ProductReceiptDetail
            {
                Product = item.Product,
                Quantity = item.Quantity,
                Weight = item.Weight,
                AllocatedWarehouseShippingCostNio = warehouseShippingAllocations[item.Product.Id]
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
                    IsSurplus = item.IsSurplus,
                    AllocatedWarehouseShippingCostNio = warehouseShippingAllocations[item.Product.Id]
                })
                .ToList(),
            TrackingNumberIds = GetReceivedTrackingNumbers(order, receiveOrderDTO)
                .Select(tracking => tracking.Id)
                .ToList()
        };
    }

    public async Task<OrderReceiptDTO> UpdateShippingCostAsync(int orderId, int receiptId, UpdateOrderReceiptDTO updateOrderReceiptDTO)
    {
        updateOrderReceiptDTO.TrackingNumbers ??= [];
        updateOrderReceiptDTO.Products ??= [];

        var order = await _context.Orders
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == orderId)
            ?? throw new AppNotFoundException($"La orden con id '{orderId}' no existe.");

        if (order.OrderStatusId == (int)OrderStatusCode.Cancelled)
        {
            throw new AppBadRequestException("No se puede corregir una recepción de una orden cancelada.");
        }

        var receipt = await _context.ProductReceipts
            .Include(item => item.ProductReceiptDetails)
            .Include(item => item.OrderTrackingNumbers)
            .FirstOrDefaultAsync(item => item.Id == receiptId && item.OrderId == orderId)
            ?? throw new AppNotFoundException($"La recepción con id '{receiptId}' no existe en la orden.");

        var affectedProductIds = receipt.ProductReceiptDetails
            .Select(detail => detail.ProductId)
            .Distinct()
            .ToList();
        var oldAllocationByProduct = receipt.ProductReceiptDetails
            .GroupBy(detail => detail.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(detail => detail.AllocatedWarehouseShippingCostNio));

        var warehouseShippingCostUsd = ResolveUpdatedWarehouseShippingCost(receipt, updateOrderReceiptDTO);
        var warehouseShippingCostNio = Math.Round(warehouseShippingCostUsd * order.ExchangeRate, 2);
        ApplyUpdatedTrackingCosts(receipt, updateOrderReceiptDTO);
        ApplyUpdatedProductWeights(receipt, updateOrderReceiptDTO);
        ApplyShippingAllocations(receipt, warehouseShippingCostNio);
        receipt.WarehouseShippingCostUsd = warehouseShippingCostUsd;

        var newAllocationByProduct = receipt.ProductReceiptDetails
            .GroupBy(detail => detail.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(detail => detail.AllocatedWarehouseShippingCostNio));

        foreach (var product in order.Products.Where(product => affectedProductIds.Contains(product.Id)))
        {
            var oldAllocation = oldAllocationByProduct[product.Id];
            var newAllocation = newAllocationByProduct[product.Id];
            product.AllocatedShippingCostNio += newAllocation - oldAllocation;
            product.TotalCostNio = product.MerchandiseTotalCostNio + product.AllocatedShippingCostNio;
            product.UnitCostNio = CalculateUnitCostNio(product);
            product.UnitCostUsd = order.ExchangeRate == 0
                ? 0
                : Math.Round(product.UnitCostNio / order.ExchangeRate, 2);
        }

        foreach (var product in order.Products)
        {
            product.TotalCostNio = product.MerchandiseTotalCostNio + product.AllocatedShippingCostNio;
        }

        order.WarehouseShippingCostUsd = await _context.ProductReceipts
            .Where(item => item.OrderId == orderId)
            .SumAsync(item => item.Id == receiptId ? warehouseShippingCostUsd : item.WarehouseShippingCostUsd);
        order.TotalCostNio = order.Products.Sum(product => product.TotalCostNio);

        await RecalculateSaleProductsAsync(affectedProductIds, order.Products);
        await SyncWarehouseShippingFinancialMovementAsync(order, receipt, warehouseShippingCostNio);

        IDbContextTransaction? transaction = null;
        try
        {
            try
            {
                transaction = await _context.BeginTransactionAsync();
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("Transactions are not supported", StringComparison.OrdinalIgnoreCase))
            {
                // EF Core InMemory does not support transactions; production providers do.
            }

            await _context.SaveChangesAsync();
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync();
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        return new OrderReceiptDTO
        {
            Id = receipt.Id,
            OrderId = receipt.OrderId,
            ReceivedDate = receipt.ReceivedDate,
            CreatedAt = receipt.CreatedAt,
            WarehouseShippingCostUsd = receipt.WarehouseShippingCostUsd,
            WarehouseShippingCostNio = receipt.WarehouseShippingCostNio,
            OrderStatusId = order.OrderStatusId,
            Products = receipt.ProductReceiptDetails
                .Select(detail => new OrderReceiptProductDTO
                {
                    ProductReceiptDetailId = detail.Id,
                    ProductId = detail.ProductId,
                    Quantity = (int)detail.Quantity,
                    Weight = detail.Weight,
                    AllocatedWarehouseShippingCostNio = detail.AllocatedWarehouseShippingCostNio
                })
                .ToList(),
            TrackingNumberIds = receipt.OrderTrackingNumbers.Select(tracking => tracking.Id).ToList()
        };
    }

    private static void EnsureOrderCanReceive(Order order, ICollection<ReceiveOrderProductDTO> products)
    {
        if (order.OrderStatusId == (int)OrderStatusCode.Cancelled)
        {
            throw new AppBadRequestException("No se puede recibir productos de una orden cancelada.");
        }

        if (order.OrderStatusId is (int)OrderStatusCode.Received or (int)OrderStatusCode.PendingRefund &&
            products.Any(product => !product.IsSurplus))
        {
            throw new AppBadRequestException("La orden no admite más recepciones normales.");
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
            if (!productDTO.IsSurplus && productDTO.Quantity > pendingQuantity)
            {
                throw new AppBadRequestException($"La cantidad recibida del producto '{product.Id}' supera la cantidad pendiente.");
            }

            receivedProducts.Add(new ReceivedProduct(
                product,
                productDTO.Quantity,
                productDTO.Weight,
                productDTO.IsSurplus));
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
        if (order.OrderStatusId == (int)OrderStatusCode.PendingRefund)
        {
            return (int)OrderStatusCode.PendingRefund;
        }

        return order.Products.All(product => product.ReceivedQuantity >= product.Quantity)
            ? (int)OrderStatusCode.Received
            : (int)OrderStatusCode.PartiallyReceived;
    }

    private static decimal CalculateReceivedAmountNio(Order order)
    {
        if (order.Products.All(product => product.ReceivedQuantity >= product.Quantity))
        {
            return order.MerchandiseTotalNio;
        }

        return Math.Round(order.Products.Sum(product =>
            product.Quantity == 0
                ? 0
                : product.MerchandiseTotalCostNio * Math.Min(product.ReceivedQuantity, product.Quantity) / product.Quantity), 2);
    }

    private static decimal CalculateUnitCostNio(Product product)
    {
        var receivedCostQuantity = Math.Max(product.Quantity, product.ReceivedQuantity);
        return receivedCostQuantity == 0 ? 0 : Math.Round(product.TotalCostNio / receivedCostQuantity, 6);
    }

    private static decimal ResolveUpdatedWarehouseShippingCost(ProductReceipt receipt, UpdateOrderReceiptDTO updateOrderReceiptDTO)
    {
        var hasTrackings = receipt.OrderTrackingNumbers.Count != 0;
        if (hasTrackings)
        {
            if (updateOrderReceiptDTO.WarehouseShippingCostUsd.HasValue)
            {
                throw new AppBadRequestException("Cuando la recepción tiene trackings, el costo debe registrarse por tracking.");
            }

            ValidateTrackingSet(receipt, updateOrderReceiptDTO.TrackingNumbers);
            if (updateOrderReceiptDTO.TrackingNumbers.Any(tracking => tracking.ShippingCostUsd < 0))
            {
                throw new AppBadRequestException("El costo de envío no puede ser negativo.");
            }

            return updateOrderReceiptDTO.TrackingNumbers.Sum(tracking => tracking.ShippingCostUsd);
        }

        if (updateOrderReceiptDTO.TrackingNumbers.Count != 0)
        {
            throw new AppBadRequestException("La recepción no tiene trackings; envíe el costo directo de envío.");
        }

        if (!updateOrderReceiptDTO.WarehouseShippingCostUsd.HasValue)
        {
            throw new AppBadRequestException("Debe enviar el nuevo costo directo de envío.");
        }

        if (updateOrderReceiptDTO.WarehouseShippingCostUsd.Value < 0)
        {
            throw new AppBadRequestException("El costo de envío no puede ser negativo.");
        }

        return updateOrderReceiptDTO.WarehouseShippingCostUsd.Value;
    }

    private static void ValidateTrackingSet(ProductReceipt receipt, ICollection<UpdateOrderReceiptTrackingNumberDTO> trackingDTOs)
    {
        var duplicateTracking = trackingDTOs
            .GroupBy(tracking => tracking.Id)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTracking is not null)
        {
            throw new AppBadRequestException("No puede enviar trackings duplicados en la corrección.");
        }

        var receiptTrackingIds = receipt.OrderTrackingNumbers.Select(tracking => tracking.Id).ToHashSet();
        var requestedTrackingIds = trackingDTOs.Select(tracking => tracking.Id).ToHashSet();
        if (!requestedTrackingIds.SetEquals(receiptTrackingIds))
        {
            throw new AppBadRequestException("Debe enviar exactamente los trackings asociados a la recepción.");
        }
    }

    private static void ApplyUpdatedTrackingCosts(ProductReceipt receipt, UpdateOrderReceiptDTO updateOrderReceiptDTO)
    {
        var shippingCostByTrackingId = updateOrderReceiptDTO.TrackingNumbers
            .ToDictionary(tracking => tracking.Id, tracking => tracking.ShippingCostUsd);

        foreach (var tracking in receipt.OrderTrackingNumbers)
        {
            if (shippingCostByTrackingId.TryGetValue(tracking.Id, out var shippingCostUsd))
            {
                tracking.ShippingCost = shippingCostUsd;
            }
        }
    }

    private static void ApplyUpdatedProductWeights(ProductReceipt receipt, UpdateOrderReceiptDTO updateOrderReceiptDTO)
    {
        if (updateOrderReceiptDTO.Products.Count == 0)
        {
            return;
        }

        var duplicateDetail = updateOrderReceiptDTO.Products
            .GroupBy(product => product.ProductReceiptDetailId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateDetail is not null)
        {
            throw new AppBadRequestException("No puede enviar detalles de producto duplicados en la corrección.");
        }

        if (updateOrderReceiptDTO.Products.Any(product => product.Weight <= 0))
        {
            throw new AppBadRequestException("El peso del producto debe ser mayor que cero.");
        }

        var receiptDetailById = receipt.ProductReceiptDetails.ToDictionary(detail => detail.Id);
        var requestedDetailIds = updateOrderReceiptDTO.Products
            .Select(product => product.ProductReceiptDetailId)
            .ToHashSet();
        if (!requestedDetailIds.SetEquals(receiptDetailById.Keys))
        {
            throw new AppBadRequestException("Debe enviar exactamente los detalles de producto de la recepción.");
        }

        foreach (var product in updateOrderReceiptDTO.Products)
        {
            receiptDetailById[product.ProductReceiptDetailId].Weight = product.Weight;
        }
    }

    private static void ApplyShippingAllocations(ProductReceipt receipt, decimal warehouseShippingCostNio)
    {
        var totalWeight = receipt.ProductReceiptDetails.Sum(detail => detail.Weight * detail.Quantity);
        var assigned = 0m;
        var details = receipt.ProductReceiptDetails.OrderBy(detail => detail.Id).ToList();

        for (var index = 0; index < details.Count; index++)
        {
            var detail = details[index];
            var allocation = index == details.Count - 1
                ? warehouseShippingCostNio - assigned
                : totalWeight == 0
                    ? 0
                    : Math.Round(warehouseShippingCostNio * detail.Weight * detail.Quantity / totalWeight, 2);

            detail.AllocatedWarehouseShippingCostNio = allocation;
            assigned += allocation;
        }

        receipt.WarehouseShippingCostNio = warehouseShippingCostNio;
    }

    private async Task RecalculateSaleProductsAsync(ICollection<int> productIds, ICollection<Product> products)
    {
        var unitCostByProductId = products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionary(product => product.Id, product => product.UnitCostNio);
        var saleProducts = await _context.SaleProducts
            .Where(saleProduct => productIds.Contains(saleProduct.ProductId))
            .ToListAsync();

        foreach (var saleProduct in saleProducts)
        {
            saleProduct.UnitCostAtSale = unitCostByProductId[saleProduct.ProductId];
            saleProduct.TotalCostAtSale = saleProduct.UnitCostAtSale * saleProduct.Quantity;
            saleProduct.GrossProfit = saleProduct.LineTotal - saleProduct.TotalCostAtSale;
        }
    }

    private async Task SyncWarehouseShippingFinancialMovementAsync(Order order, ProductReceipt receipt, decimal amountNio)
    {
        var movement = await _context.FinancialMovements
            .FirstOrDefaultAsync(item => item.ProductReceiptId == receipt.Id && item.FinancialMovementTypeId == (int)FinancialMovementTypeOption.WarehouseShippingPayment);

        if (amountNio <= 0)
        {
            if (movement is not null)
            {
                _context.FinancialMovements.Remove(movement);
            }

            return;
        }

        if (movement is null)
        {
            await _context.FinancialMovements.AddAsync(CreateWarehouseShippingMovement(order, receipt, amountNio, null, receipt.ReceivedDate));
            return;
        }

        movement.Amount = amountNio;
        movement.ExchangeRate = order.ExchangeRate;
        movement.OrderId = order.Id;
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

    private sealed record ReceivedProduct(Product Product, int Quantity, decimal Weight, bool IsSurplus);
}
