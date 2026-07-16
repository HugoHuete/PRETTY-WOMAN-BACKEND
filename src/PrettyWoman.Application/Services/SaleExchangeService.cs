using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class SaleExchangeService(
    IApplicationDbContext context,
    IInventoryService inventoryService) : ISaleExchangeService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IInventoryService _inventoryService = inventoryService;

    public async Task<IEnumerable<SaleExchangeDTO>> GetBySaleIdAsync(int saleId)
    {
        var exchanges = await _context.SaleExchanges.AsNoTracking()
            .Include(exchange => exchange.ReturnItems)
            .Include(exchange => exchange.OutboundItems)
            .Where(exchange => exchange.OriginalSaleId == saleId)
            .OrderByDescending(exchange => exchange.CreatedAt)
            .ToListAsync();
        return exchanges.Select(Map);
    }

    public async Task<int> CreateAsync(int saleId, CreateSaleExchangeDTO request)
    {
        request.ReturnItems ??= [];
        request.OutboundItems ??= [];
        request.Comments = request.Comments.NormalizeOptional();
        if (request.ReturnItems.Count == 0 || request.OutboundItems.Count == 0)
            throw new AppBadRequestException("Un cambio debe tener al menos una prenda retornada y una prenda de salida.");

        var sale = await _context.Sales
            .Include(sale => sale.Products).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(sale => sale.Id == saleId)
            ?? throw new AppNotFoundException($"La venta con id {saleId} no existe.");

        if (sale.SaleStatusId is not ((int)SaleStatusOption.SentForDelivery or (int)SaleStatusOption.Completed))
            throw new AppBadRequestException("Solo se puede crear un cambio para una venta enviada o completada, que ya no puede cancelarse.");

        await EnsureReturnedProductsWereMovedOutOfInventoryAsync(sale, request.ReturnItems);

        var existingReturns = await _context.ExchangeReturnItems
            .Include(item => item.SaleExchange)
            .Where(item => item.SaleExchange!.OriginalSaleId == saleId && item.SaleExchange.StatusId != (int)SaleExchangeStatusOption.Cancelled)
            .ToListAsync();

        foreach (var group in request.ReturnItems.GroupBy(item => item.OriginalSaleProductId))
        {
            var saleProduct = sale.Products.SingleOrDefault(item => item.Id == group.Key)
                ?? throw new AppBadRequestException("Cada prenda retornada debe pertenecer a la venta original.");
            var requestedQuantity = group.Sum(item => item.Quantity);

            if (requestedQuantity <= 0 || requestedQuantity + existingReturns.Where(item => item.OriginalSaleProductId == group.Key).Sum(item => item.Quantity) > saleProduct.Quantity)
                throw new AppBadRequestException("La cantidad a retornar excede la cantidad vendida que aun no esta en otro cambio activo.");
            // Credito por unidad que se descuenta del saldo del cambio; por defecto normalmente coincide con lo que pago la clienta.
            if (group.Any(item => item.RecognizedUnitAmount < 0 || item.RecognizedUnitAmount > saleProduct.FinalUnitPrice))
                throw new AppBadRequestException("El valor reconocido no puede ser negativo ni mayor al precio final originalmente vendido.");
        }

        var outputProductIds = request.OutboundItems.Select(item => item.ProductId).Distinct().ToList();
        var outputProducts = await _context.Products.Where(item => outputProductIds.Contains(item.Id)).ToListAsync();
        if (outputProducts.Count != outputProductIds.Count)
            throw new AppNotFoundException("Una o mas prendas de salida no existen.");

        foreach (var group in request.OutboundItems.GroupBy(item => item.ProductId))
        {
            if (group.Any(item => item.Quantity <= 0 || !Enum.IsDefined(typeof(ExchangeOutboundItemTypeOption), item.ItemTypeId)))
                throw new AppBadRequestException("Las prendas de salida tienen datos invalidos.");
            var product = outputProducts.Single(item => item.Id == group.Key);
            if (product.AvailableQuantity < group.Sum(item => item.Quantity))
                throw new AppBadRequestException($"El producto con id {product.Id} no tiene stock disponible suficiente.");
        }

        var exchange = new SaleExchange { OriginalSaleId = saleId, Comments = request.Comments };
        foreach (var item in request.ReturnItems)
        {
            var original = sale.Products.Single(product => product.Id == item.OriginalSaleProductId);
            exchange.ReturnItems.Add(new ExchangeReturnItem
            {
                OriginalSaleProductId = original.Id,
                ProductId = original.ProductId,
                Quantity = item.Quantity,
                RecognizedUnitAmount = item.RecognizedUnitAmount,
                OriginalUnitCost = original.UnitCostAtSale,
                Comments = item.Comments.NormalizeOptional()
            });
        }
        foreach (var item in request.OutboundItems)
        {
            var product = outputProducts.Single(product => product.Id == item.ProductId);
            var lineTotal = Math.Round(product.SalePrice * item.Quantity, 2);
            exchange.OutboundItems.Add(new ExchangeOutboundItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = item.Quantity,
                ItemTypeId = item.ItemTypeId,
                UnitPrice = product.SalePrice,
                UnitCost = product.UnitCostNio,
                LineTotal = lineTotal,
                TotalCost = Math.Round(product.UnitCostNio * item.Quantity, 6),
                Comments = item.Comments.NormalizeOptional()
            });
        }
        RecalculateTotals(exchange);
        ReserveOutboundItems(exchange.OutboundItems);
        await _context.SaleExchanges.AddAsync(exchange);
        await _context.SaveChangesAsync();
        return exchange.Id;
    }

    public async Task CompleteHandoverAsync(int saleId, int exchangeId)
    {
        var exchange = await GetExchangeAsync(saleId, exchangeId);
        EnsureExchangeIsActive(exchange);
        if (exchange.ReturnItems.Any(item => item.StatusId != (int)ExchangeReturnItemStatusOption.PendingHandover) ||
            exchange.OutboundItems.Any(item => item.Delivered))
            throw new AppBadRequestException("El intercambio fisico solo puede registrarse una vez y antes de entregar prendas.");

        foreach (var item in exchange.ReturnItems) HandReturnToAgency(item);
        DeliverOutboundItems(exchange.OutboundItems);
        // La unica operacion pendiente es el retorno fisico de la agencia a la tienda.
        exchange.StatusId = (int)SaleExchangeStatusOption.AwaitingReturn;
        await _context.SaveChangesAsync();
    }

    public async Task MarkReturnReceivedAsync(int saleId, int exchangeId, int returnItemId)
    {
        var exchange = await GetExchangeAsync(saleId, exchangeId);
        EnsureExchangeIsActive(exchange);
        var item = GetReturnItem(exchange, returnItemId);
        if (item.StatusId != (int)ExchangeReturnItemStatusOption.AwaitingReturn)
            throw new AppBadRequestException("La prenda no esta pendiente de retorno fisico.");
        item.StatusId = (int)ExchangeReturnItemStatusOption.Received;
        item.ReceivedAt = DateTime.UtcNow;
        UpdateCompletionStatus(exchange);
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int saleId, int exchangeId)
    {
        var exchange = await GetExchangeAsync(saleId, exchangeId);
        if (exchange.StatusId != (int)SaleExchangeStatusOption.Requested || exchange.ReturnItems.Any(item => item.StatusId != (int)ExchangeReturnItemStatusOption.PendingHandover))
            throw new AppBadRequestException("Solo se puede cancelar un cambio antes de entregar o recibir prendas.");
        ReleaseOutboundReservations(exchange.OutboundItems);
        exchange.StatusId = (int)SaleExchangeStatusOption.Cancelled;
        await _context.SaveChangesAsync();
    }

    private async Task<SaleExchange> GetExchangeAsync(int saleId, int exchangeId)
        => await _context.SaleExchanges
            .Include(exchange => exchange.ReturnItems).ThenInclude(item => item.Product)
            .Include(exchange => exchange.OutboundItems).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(exchange => exchange.Id == exchangeId && exchange.OriginalSaleId == saleId)
            ?? throw new AppNotFoundException("El cambio no existe para la venta indicada.");

    private static ExchangeReturnItem GetReturnItem(SaleExchange exchange, int returnItemId)
        => exchange.ReturnItems.SingleOrDefault(item => item.Id == returnItemId)
            ?? throw new AppNotFoundException("La prenda retornada no existe para el cambio indicado.");

    private async Task EnsureReturnedProductsWereMovedOutOfInventoryAsync(Sale sale, IEnumerable<CreateExchangeReturnItemDTO> returnItems)
    {
        var saleProductIds = returnItems.Select(item => item.OriginalSaleProductId).Distinct().ToList();
        var movedOutProductIds = await _context.InventoryMovements
            .Where(movement =>
                movement.SaleProductId.HasValue && saleProductIds.Contains(movement.SaleProductId.Value) &&
                (movement.InventoryMovementTypeId == (int)InventoryMovementTypeOption.Sale ||
                 movement.InventoryMovementTypeId == (int)InventoryMovementTypeOption.SelectionConvertedToSale) &&
                movement.FromStockBucketId == (int)InventoryStockBucketOption.Available &&
                movement.ToStockBucketId == (int)InventoryStockBucketOption.OutOfInventory)
            .Select(movement => movement.SaleProductId!.Value)
            .Distinct()
            .ToListAsync();

        if (movedOutProductIds.Count != saleProductIds.Count)
            throw new AppBadRequestException("Solo se pueden crear cambios para prendas cuya venta ya desconto inventario.");
    }

    private static void EnsureExchangeIsActive(SaleExchange exchange)
    {
        if (exchange.StatusId == (int)SaleExchangeStatusOption.Cancelled)
            throw new AppBadRequestException("No se puede modificar un cambio cancelado.");
    }

    private void HandReturnToAgency(ExchangeReturnItem item)
    {
        var product = item.Product!;
        item.StatusId = (int)ExchangeReturnItemStatusOption.AwaitingReturn;
        item.HandedToAgencyAt = DateTime.UtcNow;

        var movement = _inventoryService.Move(
            product,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Available,
            item.Quantity,
            InventoryMovementTypeOption.ExchangeReturnReceivedByAgency,
            item.HandedToAgencyAt.Value,
            "Prenda de cambio recibida por agencia; disponible y pendiente de retorno fisico.");
        movement.ExchangeReturnItem = item;
    }

    private void DeliverOutboundItems(IEnumerable<ExchangeOutboundItem> items)
    {
        foreach (var item in items.Where(item => !item.Delivered))
        {
            var product = item.Product!;
            item.Delivered = true;
            item.DeliveredAt = DateTime.UtcNow;

            var movement = _inventoryService.Move(
                product,
                InventoryStockBucketOption.Reserved,
                InventoryStockBucketOption.OutOfInventory,
                item.Quantity,
                InventoryMovementTypeOption.ExchangeReplacementDelivered,
                item.DeliveredAt.Value,
                "Prenda entregada como parte de un cambio.");
            movement.ExchangeOutboundItem = item;
        }
    }

    private void ReserveOutboundItems(IEnumerable<ExchangeOutboundItem> items)
    {
        foreach (var item in items)
        {
            var product = item.Product!;

            var movement = _inventoryService.Move(
                product,
                InventoryStockBucketOption.Available,
                InventoryStockBucketOption.Reserved,
                item.Quantity,
                InventoryMovementTypeOption.ExchangeReplacementReserved,
                DateTime.UtcNow,
                "Prenda reservada para cambio.");
            movement.ExchangeOutboundItem = item;
        }
    }

    private void ReleaseOutboundReservations(IEnumerable<ExchangeOutboundItem> items)
    {
        foreach (var item in items.Where(item => !item.Delivered))
        {
            var product = item.Product!;

            var movement = _inventoryService.Move(
                product,
                InventoryStockBucketOption.Reserved,
                InventoryStockBucketOption.Available,
                item.Quantity,
                InventoryMovementTypeOption.ExchangeReplacementReservationReleased,
                DateTime.UtcNow,
                "Reserva de cambio liberada por cancelacion.");
            movement.ExchangeOutboundItem = item;
        }
    }

    private static void RecalculateTotals(SaleExchange exchange)
    {
        // El credito total reconoce el monto acordado por cada unidad retornada, no el precio actual del catalogo.
        exchange.RecognizedReturnTotal = Math.Round(exchange.ReturnItems.Sum(item => item.RecognizedUnitAmount * item.Quantity), 2);
        exchange.OutboundItemsTotal = Math.Round(exchange.OutboundItems.Sum(item => item.LineTotal), 2);
        exchange.BalanceToCollect = exchange.OutboundItemsTotal - exchange.RecognizedReturnTotal;
    }

    private static void UpdateCompletionStatus(SaleExchange exchange)
    {
        if (exchange.OutboundItems.All(item => item.Delivered) && exchange.ReturnItems.All(item => item.StatusId is (int)ExchangeReturnItemStatusOption.Received or (int)ExchangeReturnItemStatusOption.Missing))
            exchange.StatusId = (int)SaleExchangeStatusOption.Completed;
        else if (exchange.OutboundItems.All(item => item.Delivered))
            exchange.StatusId = (int)SaleExchangeStatusOption.ReplacementDelivered;
    }

    private static SaleExchangeDTO Map(SaleExchange exchange)
    {
        var returnReversal = exchange.ReturnItems
            .Where(item => item.StatusId is (int)ExchangeReturnItemStatusOption.AwaitingReturn or (int)ExchangeReturnItemStatusOption.Received)
            .Sum(item => item.Quantity * (item.OriginalUnitCost - item.RecognizedUnitAmount));
        return new SaleExchangeDTO
        {
            Id = exchange.Id,
            OriginalSaleId = exchange.OriginalSaleId,
            StatusId = exchange.StatusId,
            StatusName = ((SaleExchangeStatusOption)exchange.StatusId).ToString(),
            RecognizedReturnTotal = exchange.RecognizedReturnTotal,
            OutboundItemsTotal = exchange.OutboundItemsTotal,
            BalanceToCollect = exchange.BalanceToCollect,
            NetGrossProfit = Math.Round(exchange.OutboundItems.Sum(item => item.LineTotal - item.TotalCost) + returnReversal, 2),
            Comments = exchange.Comments,
            ReturnItems = exchange.ReturnItems.Select(item => new SaleExchangeReturnItemDTO { Id = item.Id, OriginalSaleProductId = item.OriginalSaleProductId, ProductId = item.ProductId, Quantity = item.Quantity, RecognizedUnitAmount = item.RecognizedUnitAmount, StatusId = item.StatusId, StatusName = ((ExchangeReturnItemStatusOption)item.StatusId).ToString() }).ToList(),
            OutboundItems = exchange.OutboundItems.Select(item => new SaleExchangeOutboundItemDTO { Id = item.Id, ProductId = item.ProductId, Quantity = item.Quantity, ItemTypeId = item.ItemTypeId, UnitPrice = item.UnitPrice, UnitCost = item.UnitCost, LineTotal = item.LineTotal, TotalCost = item.TotalCost, Delivered = item.Delivered }).ToList()
        };
    }
}
