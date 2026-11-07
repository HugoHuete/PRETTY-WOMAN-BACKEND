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
            .Include(exchange => exchange.PostSaleDelivery).ThenInclude(delivery => delivery!.DeliveryAgency)
            .Include(exchange => exchange.Settlement).ThenInclude(settlement => settlement!.Tenders).ThenInclude(tender => tender.PaymentMethod)
            .Include(exchange => exchange.Settlement).ThenInclude(settlement => settlement!.Tenders).ThenInclude(tender => tender.PaymentTerminal)
            .Include(exchange => exchange.Settlement).ThenInclude(settlement => settlement!.Tenders).ThenInclude(tender => tender.FinancialMovement)
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
        if (request.ShippingChargedToClient < 0)
            throw new AppBadRequestException("Los montos de envío no pueden ser negativos.");
        bool? agencyCanCollectCashOnDelivery = null;
        if (request.DeliveryAgencyId.HasValue)
        {
            agencyCanCollectCashOnDelivery = await _context.DeliveryAgencies
                .Where(item => item.Id == request.DeliveryAgencyId.Value)
                .Select(item => (bool?)item.CanCollectCashOnDelivery)
                .FirstOrDefaultAsync();
        }
        if (request.DeliveryAgencyId.HasValue && !agencyCanCollectCashOnDelivery.HasValue)
            throw new AppNotFoundException("La agencia de envío indicada no existe.");

        var sale = await _context.Sales
            .Include(sale => sale.ProductVariants).ThenInclude(item => item.ProductVariant)
            .FirstOrDefaultAsync(sale => sale.Id == saleId)
            ?? throw new AppNotFoundException($"La venta con id {saleId} no existe.");

        if (sale.SaleStatusId is not ((int)SaleStatusOption.SentForDelivery or (int)SaleStatusOption.Completed))
            throw new AppBadRequestException("Solo se puede crear un cambio para una venta enviada o completada, que ya no puede cancelarse.");

        await EnsureReturnedProductsWereMovedOutOfInventoryAsync(sale, request.ReturnItems);

        var previouslyReturned = await GetPreviouslyReturnedQuantitiesAsync(saleId);

        foreach (var group in request.ReturnItems.GroupBy(item => item.OriginalSaleProductId))
        {
            var saleProduct = sale.ProductVariants.SingleOrDefault(item => item.Id == group.Key)
                ?? throw new AppBadRequestException("Cada prenda retornada debe pertenecer a la venta original.");
            var requestedQuantity = group.Sum(item => item.Quantity);

            if (requestedQuantity <= 0 || previouslyReturned.GetValueOrDefault(group.Key) + requestedQuantity > saleProduct.Quantity)
                throw new AppBadRequestException("La cantidad a retornar excede la cantidad vendida disponible.");
            // Credito por unidad que se descuenta del saldo del cambio; por defecto normalmente coincide con lo que pago la clienta.
            if (group.Any(item => item.RecognizedUnitAmount < 0 || item.RecognizedUnitAmount > saleProduct.FinalUnitPrice))
                throw new AppBadRequestException("El valor reconocido no puede ser negativo ni mayor al precio final originalmente vendido.");
        }

        var outputProductIds = request.OutboundItems.Select(item => item.ProductId).Distinct().ToList();
        var outputProducts = await _context.ProductVariants.Where(item => outputProductIds.Contains(item.Id)).ToListAsync();
        if (outputProducts.Count != outputProductIds.Count)
            throw new AppNotFoundException("Una o mas prendas de salida no existen.");

        foreach (var group in request.OutboundItems.GroupBy(item => item.ProductId))
        {
            if (group.Any(item => item.Quantity <= 0 || !Enum.IsDefined(typeof(ExchangeOutboundItemTypeOption), item.ItemTypeId)))
                throw new AppBadRequestException("Las prendas de salida tienen datos invalidos.");
            var productVariant = outputProducts.Single(item => item.Id == group.Key);
            if (productVariant.AvailableQuantity < group.Sum(item => item.Quantity))
                throw new AppBadRequestException($"El producto con id {productVariant.Id} no tiene stock disponible suficiente.");
        }

        var exchange = new SaleExchange { OriginalSaleId = saleId, Comments = request.Comments };
        if (request.DeliveryAgencyId.HasValue)
        {
            exchange.PostSaleDelivery = new PostSaleDelivery
            {
                DeliveryAgencyId = request.DeliveryAgencyId.Value,
                Code = request.DeliveryCode.NormalizeOptional(),
                Address = request.DeliveryAddress.NormalizeOptional(),
                StatusId = (int)PostSaleDeliveryStatusOption.Pending,
                ShippingChargedToClient = request.ShippingChargedToClient
            };
        }
        foreach (var item in request.ReturnItems)
        {
            var original = sale.ProductVariants.Single(productVariant => productVariant.Id == item.OriginalSaleProductId);
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
            var productVariant = outputProducts.Single(productVariant => productVariant.Id == item.ProductId);
            var lineTotal = Math.Round(productVariant.SalePrice * item.Quantity, 2);
            exchange.OutboundItems.Add(new ExchangeOutboundItem
            {
                ProductId = productVariant.Id,
                ProductVariant = productVariant,
                Quantity = item.Quantity,
                ItemTypeId = item.ItemTypeId,
                UnitPrice = productVariant.SalePrice,
                UnitCost = productVariant.UnitCostNio,
                LineTotal = lineTotal,
                TotalCost = Math.Round(productVariant.UnitCostNio * item.Quantity, 6),
                Comments = item.Comments.NormalizeOptional()
            });
        }
        RecalculateTotals(exchange);
        if (exchange.PostSaleDelivery is not null)
        {
            exchange.PostSaleDelivery.AmountToCollect = Math.Round(
                Math.Max(0, exchange.BalanceToCollect) + exchange.PostSaleDelivery.ShippingChargedToClient, 2);
            if (exchange.PostSaleDelivery.AmountToCollect > 0 && !agencyCanCollectCashOnDelivery.GetValueOrDefault())
                throw new AppBadRequestException("La agencia debe permitir cobro contra entrega para cobrar el saldo del cambio.");
        }
        ReserveOutboundItems(exchange.OutboundItems);
        await _context.SaleExchanges.AddAsync(exchange);
        await _context.SaveChangesAsync();
        return exchange.Id;
    }

    public async Task<SaleExchangeSettlementDTO> SettleAsync(int saleId, int exchangeId, CreateSaleExchangeSettlementDTO request)
    {
        request.Tenders ??= [];
        request.Comments = request.Comments.NormalizeOptional();
        if (request.Comments?.Length > 300)
            throw new AppBadRequestException("Los comentarios de la liquidación no pueden exceder 300 caracteres.");
        if (request.Tenders.Count == 0)
            throw new AppBadRequestException("La liquidación debe incluir al menos una línea de pago.");

        await using var transaction = await _context.BeginSalePaymentTransactionAsync(saleId);
        var exchange = await GetExchangeAsync(saleId, exchangeId);
        if (exchange.PostSaleDelivery is not null)
            throw new AppBadRequestException("Los cambios enviados por agencia se liquidan en la conciliación de la agencia.");
        if (exchange.StatusId is (int)SaleExchangeStatusOption.Requested or (int)SaleExchangeStatusOption.Cancelled)
            throw new AppBadRequestException("El cambio debe haberse entregado antes de liquidar su saldo.");
        if (exchange.Settlement is not null)
            throw new AppBadRequestException("El saldo de este cambio ya fue liquidado.");

        var expectedAmount = Math.Round(Math.Abs(exchange.BalanceToCollect), 2, MidpointRounding.AwayFromZero);
        if (expectedAmount == 0)
            throw new AppBadRequestException("No se puede liquidar un cambio sin saldo a favor de alguna de las partes.");
        if (request.Tenders.Any(item => Math.Round(item.Amount, 2, MidpointRounding.AwayFromZero) <= 0) ||
            request.Tenders.Sum(item => Math.Round(item.Amount, 2, MidpointRounding.AwayFromZero)) != expectedAmount)
        {
            throw new AppBadRequestException("La suma de las líneas de pago debe ser igual al saldo completo del cambio.");
        }

        var paymentMethodIds = request.Tenders.Select(item => item.PaymentMethodId).Distinct().ToList();
        var paymentMethods = await _context.PaymentMethods
            .Where(item => paymentMethodIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);
        if (paymentMethods.Count != paymentMethodIds.Count)
            throw new AppBadRequestException("Uno o más métodos de pago no existen.");

        foreach (var tender in request.Tenders)
        {
            var isCard = tender.PaymentMethodId == (int)PaymentMethodOption.Card;
            if (isCard != tender.PaymentTerminalId.HasValue)
                throw new AppBadRequestException(isCard
                    ? "Los pagos con tarjeta deben asociarse a una terminal de pago."
                    : "Solo los pagos con tarjeta pueden asociarse a una terminal de pago.");
        }

        var terminalIds = request.Tenders.Where(item => item.PaymentTerminalId.HasValue)
            .Select(item => item.PaymentTerminalId!.Value).Distinct().ToList();
        var terminals = await _context.PaymentTerminals
            .Where(item => terminalIds.Contains(item.Id) && item.Enabled)
            .ToDictionaryAsync(item => item.Id);
        if (terminals.Count != terminalIds.Count)
            throw new AppBadRequestException("Una o más terminales no existen o no están habilitadas.");

        var directionId = exchange.BalanceToCollect > 0
            ? (int)MovementDirectionOptions.In
            : (int)MovementDirectionOptions.Out;
        var movementDate = request.MovementDate.NormalizeToUtc() ?? DateTime.UtcNow;
        var settlement = new SaleExchangeSettlement
        {
            SaleExchangeId = exchange.Id,
            SaleExchange = exchange,
            MovementDirectionId = directionId,
            Amount = expectedAmount,
            MovementDate = movementDate,
            Comments = request.Comments
        };
        var exchangeRate = await GetExchangeRateAsync(movementDate);

        foreach (var tenderRequest in request.Tenders)
        {
            var amount = Math.Round(tenderRequest.Amount, 2, MidpointRounding.AwayFromZero);
            var terminal = tenderRequest.PaymentTerminalId.HasValue
                ? terminals[tenderRequest.PaymentTerminalId.Value]
                : null;
            var commissionPercentage = directionId == (int)MovementDirectionOptions.In
                ? terminal?.ComissionPercentage ?? 0
                : 0;
            var commissionAmount = Math.Round(amount * commissionPercentage / 100, 2, MidpointRounding.AwayFromZero);
            var incomeTaxPercentage = directionId == (int)MovementDirectionOptions.In
                ? terminal?.IncomeTaxPercentage ?? 0
                : 0;
            var incomeTaxAmount = Math.Round((amount - commissionAmount) * incomeTaxPercentage / 100, 2, MidpointRounding.AwayFromZero);
            var financialAmount = directionId == (int)MovementDirectionOptions.In
                ? amount - commissionAmount - incomeTaxAmount
                : amount;
            if (commissionPercentage < 0 || incomeTaxPercentage < 0 || financialAmount <= 0)
                throw new AppBadRequestException("La configuración de la terminal genera un monto neto inválido.");

            var tender = new SaleExchangeSettlementTender
            {
                Settlement = settlement,
                PaymentMethodId = tenderRequest.PaymentMethodId,
                PaymentMethod = paymentMethods[tenderRequest.PaymentMethodId],
                PaymentTerminalId = tenderRequest.PaymentTerminalId,
                PaymentTerminal = terminal,
                Amount = amount,
                CommissionPercentage = commissionPercentage,
                CommissionAmount = commissionAmount,
                IncomeTaxPercentage = incomeTaxPercentage,
                IncomeTaxAmount = incomeTaxAmount
            };
            settlement.Tenders.Add(tender);
            tender.FinancialMovement = new FinancialMovement
            {
                Description = directionId == (int)MovementDirectionOptions.In
                    ? $"Cobro del cambio #{exchange.Id}."
                    : $"Reembolso del cambio #{exchange.Id}.",
                MovementDate = movementDate,
                MovementDirectionId = directionId,
                FinancialMovementTypeId = (int)FinancialMovementTypeOption.SaleExchangeSettlement,
                Amount = financialAmount,
                ExchangeRate = exchangeRate,
                Comments = request.Comments,
                SaleExchangeSettlementTender = tender
            };
        }

        await _context.SaleExchangeSettlements.AddAsync(settlement);
        await _context.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();

        return MapSettlement(settlement);
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
        if (exchange.PostSaleDelivery is not null)
            exchange.PostSaleDelivery.StatusId = (int)PostSaleDeliveryStatusOption.InTransit;
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
        if (exchange.StatusId == (int)SaleExchangeStatusOption.Completed && exchange.PostSaleDelivery is not null)
        {
            exchange.PostSaleDelivery.StatusId = (int)PostSaleDeliveryStatusOption.Completed;
            exchange.PostSaleDelivery.ReceivedAt = item.ReceivedAt;
        }
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int saleId, int exchangeId)
    {
        var exchange = await GetExchangeAsync(saleId, exchangeId);
        if (exchange.StatusId != (int)SaleExchangeStatusOption.Requested || exchange.ReturnItems.Any(item => item.StatusId != (int)ExchangeReturnItemStatusOption.PendingHandover))
            throw new AppBadRequestException("Solo se puede cancelar un cambio antes de entregar o recibir prendas.");
        ReleaseOutboundReservations(exchange.OutboundItems);
        exchange.StatusId = (int)SaleExchangeStatusOption.Cancelled;
        if (exchange.PostSaleDelivery is not null)
            exchange.PostSaleDelivery.StatusId = (int)PostSaleDeliveryStatusOption.Cancelled;
        await _context.SaveChangesAsync();
    }

    private async Task<SaleExchange> GetExchangeAsync(int saleId, int exchangeId)
        => await _context.SaleExchanges
            .Include(exchange => exchange.ReturnItems).ThenInclude(item => item.ProductVariant)
            .Include(exchange => exchange.OutboundItems).ThenInclude(item => item.ProductVariant)
            .Include(exchange => exchange.PostSaleDelivery).ThenInclude(delivery => delivery!.DeliveryAgency)
            .Include(exchange => exchange.Settlement).ThenInclude(settlement => settlement!.Tenders).ThenInclude(tender => tender.PaymentMethod)
            .Include(exchange => exchange.Settlement).ThenInclude(settlement => settlement!.Tenders).ThenInclude(tender => tender.PaymentTerminal)
            .Include(exchange => exchange.Settlement).ThenInclude(settlement => settlement!.Tenders).ThenInclude(tender => tender.FinancialMovement)
            .FirstOrDefaultAsync(exchange => exchange.Id == exchangeId && exchange.OriginalSaleId == saleId)
            ?? throw new AppNotFoundException("El cambio no existe para la venta indicada.");

    private async Task<decimal> GetExchangeRateAsync(DateTime movementDate)
    {
        var exchangeRate = await _context.DollarExchangeRates
            .Where(item => item.Enabled && item.StartDate <= movementDate)
            .OrderByDescending(item => item.StartDate)
            .Select(item => (decimal?)item.StoreRate)
            .FirstOrDefaultAsync();
        return exchangeRate ?? throw new AppBadRequestException("No existe una tasa de cambio de tienda habilitada para la fecha de la liquidación.");
    }

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
                 movement.InventoryMovementTypeId == (int)InventoryMovementTypeOption.ReservationConvertedToSale ||
                 movement.InventoryMovementTypeId == (int)InventoryMovementTypeOption.SelectionConvertedToSale) &&
                movement.ToStockBucketId == (int)InventoryStockBucketOption.OutOfInventory)
            .Select(movement => movement.SaleProductId!.Value)
            .Distinct()
            .ToListAsync();

        if (movedOutProductIds.Count != saleProductIds.Count)
            throw new AppBadRequestException("Solo se pueden crear cambios para prendas cuya venta ya desconto inventario.");
    }

    private async Task<Dictionary<int, int>> GetPreviouslyReturnedQuantitiesAsync(int saleId)
    {
        // Cambios y devoluciones consumen la misma cantidad vendida original; se validan juntos
        // para que una unidad no pueda entrar dos veces al ciclo de retorno.
        var returns = await _context.SaleReturnItems
            .Include(item => item.SaleReturn)
            .Where(item => item.SaleReturn!.OriginalSaleId == saleId &&
                           item.SaleReturn.StatusId != (int)SaleReturnStatusOption.Cancelled)
            .GroupBy(item => item.OriginalSaleProductId)
            .Select(group => new { Id = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToListAsync();
        var exchanges = await _context.ExchangeReturnItems
            .Include(item => item.SaleExchange)
            .Where(item => item.SaleExchange!.OriginalSaleId == saleId &&
                           item.SaleExchange.StatusId != (int)SaleExchangeStatusOption.Cancelled)
            .GroupBy(item => item.OriginalSaleProductId)
            .Select(group => new { Id = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToListAsync();

        return returns.Concat(exchanges)
            .GroupBy(item => item.Id)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
    }

    private static void EnsureExchangeIsActive(SaleExchange exchange)
    {
        if (exchange.StatusId == (int)SaleExchangeStatusOption.Cancelled)
            throw new AppBadRequestException("No se puede modificar un cambio cancelado.");
    }

    private void HandReturnToAgency(ExchangeReturnItem item)
    {
        var productVariant = item.ProductVariant!;
        item.StatusId = (int)ExchangeReturnItemStatusOption.AwaitingReturn;
        item.HandedToAgencyAt = DateTime.UtcNow;

        var movement = _inventoryService.Move(
            productVariant,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Available,
            item.Quantity,
            InventoryMovementTypeOption.ExchangeReturnReceivedByAgency,
            item.HandedToAgencyAt.Value,
            "Prenda de cambio recibida por agencia; disponible y pendiente de retorno fisico.");
        movement.ExchangeReturnItem = item;
        // La línea original permite descontar este retorno del compromiso neto de la venta.
        movement.SaleProductId = item.OriginalSaleProductId;
    }

    private void DeliverOutboundItems(IEnumerable<ExchangeOutboundItem> items)
    {
        foreach (var item in items.Where(item => !item.Delivered))
        {
            var productVariant = item.ProductVariant!;
            item.Delivered = true;
            item.DeliveredAt = DateTime.UtcNow;

            var movement = _inventoryService.Move(
                productVariant,
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
            var productVariant = item.ProductVariant!;

            var movement = _inventoryService.Move(
                productVariant,
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
            var productVariant = item.ProductVariant!;

            var movement = _inventoryService.Move(
                productVariant,
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
        {
            if (exchange.StatusId != (int)SaleExchangeStatusOption.Completed)
                exchange.CompletedAt = DateTime.UtcNow;

            exchange.StatusId = (int)SaleExchangeStatusOption.Completed;
        }
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
            PostSaleDelivery = exchange.PostSaleDelivery is null ? null : new PostSaleDeliveryDTO { Id = exchange.PostSaleDelivery.Id, SaleReturnId = exchange.PostSaleDelivery.SaleReturnId, SaleExchangeId = exchange.PostSaleDelivery.SaleExchangeId, DeliveryAgencyId = exchange.PostSaleDelivery.DeliveryAgencyId, DeliveryAgencyName = exchange.PostSaleDelivery.DeliveryAgency?.Name, StatusId = exchange.PostSaleDelivery.StatusId, StatusName = ((PostSaleDeliveryStatusOption)exchange.PostSaleDelivery.StatusId).ToString(), Code = exchange.PostSaleDelivery.Code, Address = exchange.PostSaleDelivery.Address, ShippingChargedToClient = exchange.PostSaleDelivery.ShippingChargedToClient, ShippingPaidToAgency = exchange.PostSaleDelivery.ShippingPaidToAgency, AmountToCollect = exchange.PostSaleDelivery.AmountToCollect, AmountCollectedNio = exchange.PostSaleDelivery.AmountCollectedNio, AmountCollectedUsd = exchange.PostSaleDelivery.AmountCollectedUsd, ChangeGivenNio = exchange.PostSaleDelivery.ChangeGivenNio, CollectionExchangeRate = exchange.PostSaleDelivery.CollectionExchangeRate, PickedUpAt = exchange.PostSaleDelivery.PickedUpAt, ReceivedAt = exchange.PostSaleDelivery.ReceivedAt },
            Settlement = exchange.Settlement is null ? null : MapSettlement(exchange.Settlement),
            ReturnItems = exchange.ReturnItems.Select(item => new SaleExchangeReturnItemDTO { Id = item.Id, OriginalSaleProductId = item.OriginalSaleProductId, ProductId = item.ProductId, Quantity = item.Quantity, RecognizedUnitAmount = item.RecognizedUnitAmount, StatusId = item.StatusId, StatusName = ((ExchangeReturnItemStatusOption)item.StatusId).ToString() }).ToList(),
            OutboundItems = exchange.OutboundItems.Select(item => new SaleExchangeOutboundItemDTO { Id = item.Id, ProductId = item.ProductId, Quantity = item.Quantity, ItemTypeId = item.ItemTypeId, UnitPrice = item.UnitPrice, UnitCost = item.UnitCost, LineTotal = item.LineTotal, TotalCost = item.TotalCost, Delivered = item.Delivered }).ToList()
        };
    }

    private static SaleExchangeSettlementDTO MapSettlement(SaleExchangeSettlement settlement)
        => new()
        {
            Id = settlement.Id,
            MovementDirectionId = settlement.MovementDirectionId,
            MovementDirectionName = ((MovementDirectionOptions)settlement.MovementDirectionId).ToString(),
            Amount = settlement.Amount,
            MovementDate = settlement.MovementDate,
            Comments = settlement.Comments,
            Tenders = settlement.Tenders.Select(item => new SaleExchangeSettlementTenderDTO
            {
                Id = item.Id,
                PaymentMethodId = item.PaymentMethodId,
                PaymentMethodName = item.PaymentMethod?.Name,
                PaymentTerminalId = item.PaymentTerminalId,
                PaymentTerminalName = item.PaymentTerminal?.Name,
                Amount = item.Amount,
                CommissionAmount = item.CommissionAmount,
                IncomeTaxAmount = item.IncomeTaxAmount,
                FinancialMovementId = item.FinancialMovement?.Id
            }).ToList()
        };
}
