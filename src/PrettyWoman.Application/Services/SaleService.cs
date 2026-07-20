using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Calculations;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class SaleService(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ISalePaymentMovementService paymentMovementService,
    ISaleDeliveryService deliveryService,
    IInventoryService inventoryService) : ISaleService
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ISalePaymentMovementService _paymentMovementService = paymentMovementService;
    private readonly ISaleDeliveryService _deliveryService = deliveryService;
    private readonly IInventoryService _inventoryService = inventoryService;

    public async Task<PaginatedResult<SaleDTO>> GetAllAsync(SaleQueryDTO query)
    {
        NormalizePagination(query);

        var salesQuery = ApplySaleFilters(_context.Sales
            .AsNoTracking()
            .AsQueryable(), query);

        var totalCount = await salesQuery.CountAsync();
        var sales = await salesQuery
            .Include(sale => sale.SaleChannel)
            .Include(sale => sale.SaleStatus)
            .Include(sale => sale.SalePaymentStatus)
            .Include(sale => sale.Client)
            .OrderByDescending(sale => sale.SaleDate)
            .ThenByDescending(sale => sale.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PaginatedResult<SaleDTO>
        {
            Items = sales.Select(MapSaleDTO).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SaleDTO> GetByIdAsync(int id)
    {
        var sale = await GetSaleWithDetailsAsync(id, asNoTracking: true);
        return MapSaleDTO(sale);
    }

    public async Task<int> CreateAsync(CreateSaleDTO createSaleDTO)
    {
        NormalizeSaleFields(createSaleDTO);
        await ValidateSaleRequestAsync(createSaleDTO);

        // Se congelan precios, costos y descuentos antes de persistir la venta.
        var products = await LoadSaleProductsAsync(createSaleDTO.Products, createSaleDTO.SelectionProducts);
        var saleProducts = CreateSaleProducts(createSaleDTO.Products, products);
        var payments = await _paymentMovementService.CreateInitialAsync(createSaleDTO.PaymentMovements);
        // Los nuevos totales se validan contra pagos ya aplicados; un exceso queda como reembolso pendiente.
        var totals = CalculateSaleTotals(saleProducts);
        var paymentTotal = SalePaymentMovementRules.CalculateProductTotal(payments);
        SalePaymentMovementRules.EnsureAllowedProductTotal(createSaleDTO.SaleChannelId, totals.Total, paymentTotal);

        var sale = new Sale
        {
            SaleDate = createSaleDTO.SaleDate ?? DateTime.UtcNow,
            SaleChannelId = createSaleDTO.SaleChannelId,
            SaleStatusId = createSaleDTO.SaleStatusId,
            SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(totals.Total, paymentTotal),
            UserId = ResolveUserId(),
            Subtotal = totals.Subtotal,
            TotalDiscount = totals.TotalDiscount,
            Total = totals.Total,
            Comments = createSaleDTO.Comments,
            ClientId = createSaleDTO.ClientId,
            Products = saleProducts,
            ProductHolds = CreateSelectionHolds(createSaleDTO.SelectionProducts, products),
            PaymentMovements = payments
        };

        CompleteInStoreSaleWhenPaid(sale);

        ApplyInventoryForSaleStatus(sale);

        ApplySelectionHoldsInventory(sale.ProductHolds);

        await _paymentMovementService.CreateFinancialMovementsAsync(payments);
        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

        return sale.Id;
    }

    public async Task PatchHeaderAsync(int id, PatchSaleHeaderDTO patchSaleHeaderDTO)
    {
        NormalizeSaleFields(patchSaleHeaderDTO);
        await ValidateSaleHeaderPatchAsync(patchSaleHeaderDTO);

        // Esta ruta cambia solo cabecera; productos, pagos y envios conservan su historial.
        var sale = await GetSaleWithDetailsAsync(id, asNoTracking: false);
        EnsureSaleHeaderCanBePatched(sale);

        if (patchSaleHeaderDTO.HasSaleChannelId)
        {
            SalePaymentMovementRules.EnsureAllowedProductTotal(
                patchSaleHeaderDTO.SaleChannelId!.Value,
                sale.Total,
                SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements),
                allowOverpayment: true);

            await _deliveryService.EnsureSaleChannelCanBeChangedAsync(
                sale.Id,
                patchSaleHeaderDTO.SaleChannelId.Value);

            sale.SaleChannelId = patchSaleHeaderDTO.SaleChannelId.Value;
        }

        if (patchSaleHeaderDTO.HasSaleDate)
        {
            var previousSaleDate = sale.SaleDate;
            sale.SaleDate = patchSaleHeaderDTO.SaleDate!.Value;
            await SyncInventoryMovementDatesAsync(sale, previousSaleDate);
        }

        if (patchSaleHeaderDTO.HasClientId) sale.ClientId = patchSaleHeaderDTO.ClientId;
        if (patchSaleHeaderDTO.HasComments) sale.Comments = patchSaleHeaderDTO.Comments;

        await _context.SaveChangesAsync();
    }

    public async Task ReplaceProductsAsync(int id, ReplaceSaleProductsDTO replaceSaleProductsDTO)
    {
        NormalizeSaleFields(replaceSaleProductsDTO);
        await ValidateSaleProductsReplacementAsync(replaceSaleProductsDTO);

        var sale = await GetSaleWithDetailsAsync(id, asNoTracking: false);
        EnsureSaleProductsCanBeReplaced(sale);
        ValidateReplacementLineReferences(sale, replaceSaleProductsDTO.Products);
        await ValidateReplacementPostSaleReferencesAsync(sale, replaceSaleProductsDTO.Products);

        // El request representa el estado final completo de la venta. Las líneas existentes que
        // no aparezcan mediante SaleProductId se consideran eliminadas.
        var saleProductIds = sale.Products.Select(product => product.Id).ToList();
        var inventoryMovements = await _context.InventoryMovements
            .Where(movement => movement.SaleProductId.HasValue && saleProductIds.Contains(movement.SaleProductId.Value))
            .ToListAsync();
        var inventoryBucket = GetInventoryBucketForSaleStatus(sale.SaleStatusId);
        var requestedProducts = replaceSaleProductsDTO.Products.Cast<CreateSaleProductDTO>().ToList();
        var products = await LoadProductsByIdsAsync(requestedProducts.Select(product => product.ProductId));

        var existingLines = sale.Products.ToDictionary(product => product.Id);
        var retainedLineIds = replaceSaleProductsDTO.Products
            .Where(product => product.SaleProductId.HasValue)
            .Select(product => product.SaleProductId!.Value)
            .ToHashSet();
        var removedLines = sale.Products.Where(product => !retainedLineIds.Contains(product.Id)).ToList();
        var finalLines = new List<SaleProduct>();
        var requestedLineStates = new List<(
            SaleProduct Line,
            ReplaceSaleProductDTO Request,
            int CurrentCommittedQuantity,
            int TargetCommittedQuantity)>();

        // Una línea con SaleProductId conserva su identidad y sus valores históricos de precio y costo.
        // Una línea sin SaleProductId es un producto nuevo y toma snapshots desde el catálogo actual.
        foreach (var request in replaceSaleProductsDTO.Products)
        {
            SaleProduct line;
            var currentCommittedQuantity = 0;
            var targetCommittedQuantity = request.Quantity;
            if (request.SaleProductId.HasValue)
            {
                line = existingLines[request.SaleProductId.Value];

                // Se usa la cantidad neta del bucket actual, no solo la cantidad anterior de la línea,
                // porque una transición o devolución previa pudo haber movido parte de esas unidades.
                if (inventoryBucket.HasValue)
                    currentCommittedQuantity = CalculateQuantityInBucket(line.Id, inventoryMovements, inventoryBucket.Value);

                // La cantidad de la línea sigue siendo histórica/comercial. El compromiso físico
                // excluye las unidades que ya regresaron mediante una devolución o un cambio.
                targetCommittedQuantity -= CalculateReceivedReturnQuantity(line.Id, inventoryMovements);
                UpdateExistingSaleProduct(line, request);
            }
            else
            {
                line = CreateSaleProducts([request], products).Single();
            }

            finalLines.Add(line);
            requestedLineStates.Add((line, request, currentCommittedQuantity, targetCommittedQuantity));
        }

        var totals = CalculateSaleTotals(finalLines);
        var paymentTotal = SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements);
        SalePaymentMovementRules.EnsureAllowedProductTotal(sale.SaleChannelId, totals.Total, paymentTotal, allowOverpayment: true);

        // La disponibilidad se valida con el cambio neto: las unidades que se liberarán en esta
        // operación también pueden cubrir aumentos o productos nuevos de la misma variante.
        ValidateReplacementInventoryAvailability(
            inventoryBucket,
            products,
            requestedLineStates,
            removedLines,
            inventoryMovements);

        // Primero se liberan las cantidades reducidas o eliminadas para que puedan cubrir aumentos
        // y líneas nuevas de la misma variante dentro de esta unidad de trabajo.
        foreach (var state in requestedLineStates.Where(state =>
                     inventoryBucket.HasValue && state.CurrentCommittedQuantity > state.TargetCommittedQuantity))
        {
            var movement = _inventoryService.Move(
                state.Line.Product!,
                inventoryBucket!.Value,
                InventoryStockBucketOption.Available,
                state.CurrentCommittedQuantity - state.TargetCommittedQuantity,
                GetReleaseMovementType(inventoryBucket.Value),
                DateTime.UtcNow,
                "Cantidad de la linea de venta reducida.");
            movement.SaleProduct = state.Line;
        }

        // Se identifica cuantas unidades hay que devolver a inventario de los productos eliminados, si es que ya habían salido.
        foreach (var removedLine in removedLines)
        {
            var committedQuantity = inventoryBucket.HasValue
                ? CalculateQuantityInBucket(removedLine.Id, inventoryMovements, inventoryBucket.Value)
                : 0;
            if (committedQuantity > 0)
            {
                // Se hace un movimiento para reponer unidades disponibles, pero luego se elimina el movimiento porque la línea ya no existe.
                var movement = _inventoryService.Move(
                    removedLine.Product!,
                    inventoryBucket!.Value,
                    InventoryStockBucketOption.Available,
                    committedQuantity,
                    GetReleaseMovementType(inventoryBucket.Value),
                    DateTime.UtcNow,
                    "Linea eliminada al corregir los productos de la venta.");

                // La línea y su historial se eliminan juntos; este movimiento solo aplica la
                // transición validada antes de retirar las entidades anteriores.
                _context.InventoryMovements.Remove(movement);
            }
        }

        var removedLineIds = removedLines.Select(line => line.Id).ToHashSet();
        _context.InventoryMovements.RemoveRange(inventoryMovements.Where(movement =>
            movement.SaleProductId.HasValue && removedLineIds.Contains(movement.SaleProductId.Value)));
        _context.SaleProducts.RemoveRange(removedLines);
        sale.Products = finalLines;

        // Después de liberar inventario, ahora falta restar disponibilidad de los productos nuevos o con mayor cantidad. Las líneas que no
        // cambiaron tienen diferencia cero y, por tanto, no generan movimientos nuevos.
        foreach (var state in requestedLineStates.Where(_ => inventoryBucket.HasValue))
        {
            var quantityToMove = state.TargetCommittedQuantity - state.CurrentCommittedQuantity;
            if (quantityToMove <= 0) continue;

            var movement = _inventoryService.Move(
                state.Line.Product!,
                InventoryStockBucketOption.Available,
                inventoryBucket!.Value,
                quantityToMove,
                GetCommitmentMovementType(inventoryBucket.Value),
                sale.SaleDate,
                state.Request.SaleProductId.HasValue
                    ? "Cantidad de la linea de venta aumentada."
                    : "Producto agregado a la venta.");
            movement.SaleProduct = state.Line;
        }

        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(totals.Total, paymentTotal);
        sale.Subtotal = totals.Subtotal;
        sale.TotalDiscount = totals.TotalDiscount;
        sale.Total = totals.Total;
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, sale.PaymentMovements);

        await _context.SaveChangesAsync();
    }

    public async Task AddSelectionHoldsAsync(int saleId, List<CreateSaleSelectionProductDTO> selectionProducts)
    {
        selectionProducts ??= [];
        await ValidateSelectionProductRequestAsync(selectionProducts);

        var sale = await GetSaleWithDetailsAsync(saleId, asNoTracking: false);
        if (sale.SaleStatusId is (int)SaleStatusOption.Cancelled or (int)SaleStatusOption.Completed ||
            sale.Deliveries.Any(delivery => delivery.DeliveryStatusId != (int)DeliveryStatusCode.Pending))
            throw new AppBadRequestException("Solo se pueden agregar prendas para seleccion antes de enviar la venta.");

        var products = await LoadSaleProductsAsync([], selectionProducts);
        var holds = CreateSelectionHolds(selectionProducts, products);
        sale.ProductHolds.AddRange(holds);
        ApplySelectionHoldsInventory(holds);
        await _context.SaveChangesAsync();
    }

    public async Task ResolveSelectionHoldAsync(int saleId, int holdId, ResolveSelectionHoldDTO resolution)
    {
        resolution.Comments = resolution.Comments.NormalizeOptional();
        var sale = await GetSaleWithDetailsAsync(saleId, asNoTracking: false);
        var hold = sale.ProductHolds.SingleOrDefault(item => item.Id == holdId)
            ?? throw new AppNotFoundException($"La prenda en seleccion con id {holdId} no existe para la venta indicada.");
        if (hold.ProductHoldStatusId != (int)ProductHoldStatusOption.Active)
            throw new AppBadRequestException("Solo se puede resolver una prenda que esta enviada para seleccion.");

        hold.Comments = resolution.Comments ?? hold.Comments;
        hold.ResolvedAt = DateTime.UtcNow;
        var product = hold.Product!;
        if (resolution.Selected)
        {
            await ValidateSaleProductRequestAsync(new CreateSaleProductDTO
            {
                ProductId = product.Id,
                Quantity = hold.Quantity,
                DiscountAmount = resolution.DiscountAmount,
                DiscountSourceId = resolution.DiscountSourceId,
                DiscountCampaignId = resolution.DiscountCampaignId
            });

            var saleProduct = CreateSaleProducts(
                [new CreateSaleProductDTO
                {
                    ProductId = product.Id,
                    Quantity = hold.Quantity,
                    DiscountAmount = resolution.DiscountAmount,
                    DiscountSourceId = resolution.DiscountSourceId,
                    DiscountCampaignId = resolution.DiscountCampaignId
                }],
                [product]).Single();
            sale.Products.Add(saleProduct);
            hold.ProductHoldStatusId = (int)ProductHoldStatusOption.ConvertedToSale;
            var movement = _inventoryService.Move(
                product,
                InventoryStockBucketOption.Unavailable,
                InventoryStockBucketOption.OutOfInventory,
                hold.Quantity,
                InventoryMovementTypeOption.SelectionConvertedToSale,
                DateTime.UtcNow,
                "Prenda de seleccion convertida a venta.");
            movement.ProductHold = hold;
            movement.SaleProduct = saleProduct;
        }
        else
        {
            hold.ProductHoldStatusId = (int)ProductHoldStatusOption.AwaitingReturn;
            var movement = _inventoryService.Move(
                product,
                InventoryStockBucketOption.Unavailable,
                InventoryStockBucketOption.Available,
                hold.Quantity,
                InventoryMovementTypeOption.SelectionReturned,
                DateTime.UtcNow,
                "Prenda no seleccionada; pendiente de retorno fisico.");
            movement.ProductHold = hold;
        }

        RecalculateSaleTotals(sale);
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, sale.PaymentMovements);
        await _context.SaveChangesAsync();
    }

    public async Task MarkSelectionHoldAsReturnedAsync(int saleId, int holdId)
    {
        var sale = await GetSaleWithDetailsAsync(saleId, asNoTracking: false);
        var hold = sale.ProductHolds.SingleOrDefault(item => item.Id == holdId)
            ?? throw new AppNotFoundException($"La prenda en seleccion con id {holdId} no existe para la venta indicada.");
        if (hold.ProductHoldStatusId != (int)ProductHoldStatusOption.AwaitingReturn)
            throw new AppBadRequestException("Solo se puede registrar el retorno de una prenda pendiente de devolver.");

        hold.ProductHoldStatusId = (int)ProductHoldStatusOption.NotSelected;
        hold.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public Task<int> CreateDeliveryAsync(int saleId, CreateSaleDeliveryDTO delivery)
        => _deliveryService.CreateAsync(saleId, delivery);

    public Task UpdateDeliveryAsync(int saleId, int deliveryId, PatchSaleDeliveryDTO delivery)
        => _deliveryService.PatchAsync(saleId, deliveryId, delivery);

    public Task MarkDeliveryAsSentAsync(int saleId, int deliveryId)
        => _deliveryService.MarkAsSentAsync(saleId, deliveryId);

    public Task MarkDeliveryAsDeliveredPendingSelectionAsync(int saleId, int deliveryId)
        => _deliveryService.MarkAsDeliveredPendingSelectionAsync(saleId, deliveryId);

    public Task MarkDeliveryAsCompletedAsync(int saleId, int deliveryId)
        => _deliveryService.MarkAsCompletedAsync(saleId, deliveryId);

    public Task MarkDeliveryAsFailedAsync(int saleId, int deliveryId)
        => _deliveryService.MarkAsFailedAsync(saleId, deliveryId);

    public Task CancelDeliveryAsync(int saleId, int deliveryId)
        => _deliveryService.CancelAsync(saleId, deliveryId);

    public async Task CancelAsync(int id)
    {
        var sale = await GetSaleWithDetailsAsync(id, asNoTracking: false);
        EnsureSaleCanBeCancelled(sale);

        // Los cobros se reembolsan por sus movimientos para conservar el medio de pago y su movimiento financiero.
        if (HasOutstandingPayments(sale))
            throw new AppBadRequestException("No se puede cancelar una venta con pagos pendientes de reembolso.");

        foreach (var delivery in sale.Deliveries.Where(delivery => delivery.DeliveryStatusId == (int)DeliveryStatusCode.Pending))
        {
            delivery.DeliveryStatusId = (int)DeliveryStatusCode.Cancelled;
        }

        if (GetInventoryBucketForSaleStatus(sale.SaleStatusId).HasValue)
        {
            var saleProductIds = sale.Products.Select(product => product.Id).ToList();
            var inventoryMovements = await _context.InventoryMovements
                .Where(movement => movement.SaleProductId.HasValue && saleProductIds.Contains(movement.SaleProductId.Value))
                .ToListAsync();
            RevertInventoryForCancelledSale(sale, inventoryMovements);
        }

        ReleaseActiveSelectionHoldsForCancelledSale(sale);

        sale.SaleStatusId = (int)SaleStatusOption.Cancelled;
        await _context.SaveChangesAsync();
    }

    public async Task<int> AddPaymentMovementAsync(int saleId, CreateSalePaymentMovementDTO paymentMovement)
    {
        var paymentMovementId = await _paymentMovementService.AddAsync(saleId, paymentMovement);
        await SynchronizeInStoreSaleCompletionAsync(saleId);
        return paymentMovementId;
    }

    public async Task UpdatePaymentMovementAsync(int saleId, int paymentMovementId, UpdateSalePaymentMovementDTO paymentMovement)
    {
        await _paymentMovementService.PatchAsync(saleId, paymentMovementId, paymentMovement);
        await SynchronizeInStoreSaleCompletionAsync(saleId);
    }

    public async Task<int> RefundPaymentMovementAsync(int saleId, int paymentMovementId, RefundSalePaymentMovementDTO refund)
    {
        var refundPaymentMovementId = await _paymentMovementService.RefundAsync(saleId, paymentMovementId, refund);
        await SynchronizeInStoreSaleCompletionAsync(saleId);
        return refundPaymentMovementId;
    }

    private async Task<Sale> GetSaleWithDetailsAsync(int id, bool asNoTracking)
    {
        var query = _context.Sales
            .Include(sale => sale.SaleChannel)
            .Include(sale => sale.SaleStatus)
            .Include(sale => sale.SalePaymentStatus)
            .Include(sale => sale.Client)
            .Include(sale => sale.Products).ThenInclude(saleProduct => saleProduct.Product)
            .Include(sale => sale.Products).ThenInclude(saleProduct => saleProduct.DiscountSource)
            .Include(sale => sale.ProductHolds).ThenInclude(hold => hold.Product)
            .Include(sale => sale.ProductHolds).ThenInclude(hold => hold.ProductHoldStatus)
            .Include(sale => sale.PaymentMovements).ThenInclude(payment => payment.PaymentMethod)
            .Include(sale => sale.PaymentMovements).ThenInclude(payment => payment.PaymentTerminal)
            .Include(sale => sale.Deliveries).ThenInclude(delivery => delivery.Municipality)
            .Include(sale => sale.Deliveries).ThenInclude(delivery => delivery.DeliveryAgency)
            .Include(sale => sale.Deliveries).ThenInclude(delivery => delivery.DeliveryStatus)
            .AsQueryable();

        if (asNoTracking) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(sale => sale.Id == id)
            ?? throw new AppNotFoundException($"La venta con id {id} no existe.");
    }

    private async Task ValidateSaleRequestAsync(CreateSaleDTO saleDTO)
    {
        if (!await _context.SaleChannels.AnyAsync(channel => channel.Id == saleDTO.SaleChannelId))
            throw new AppNotFoundException($"El canal de venta con id {saleDTO.SaleChannelId} no existe.");

        await ValidateOptionalReferencesAsync(saleDTO.ClientId);
        await ValidateRequestedSaleStatusAsync(saleDTO.SaleStatusId);
        await ValidateProductRequestAsync(saleDTO.Products, saleDTO.SelectionProducts, requireAtLeastOne: true);
    }

    private async Task ValidateSaleHeaderPatchAsync(PatchSaleHeaderDTO saleDTO)
    {
        if (saleDTO.HasSaleDate && !saleDTO.SaleDate.HasValue)
            throw new AppBadRequestException("La fecha de la venta no puede ser nula.");
        if (saleDTO.HasSaleChannelId && !saleDTO.SaleChannelId.HasValue)
            throw new AppBadRequestException("El canal de venta no puede ser nulo.");
        if (saleDTO.SaleChannelId.HasValue && !await _context.SaleChannels.AnyAsync(channel => channel.Id == saleDTO.SaleChannelId.Value))
            throw new AppNotFoundException($"El canal de venta con id {saleDTO.SaleChannelId.Value} no existe.");

        await ValidateOptionalReferencesAsync(saleDTO.ClientId);
    }

    private async Task ValidateSaleProductsReplacementAsync(ReplaceSaleProductsDTO saleDTO)
        => await ValidateProductRequestAsync(
            saleDTO.Products.Cast<CreateSaleProductDTO>().ToList(),
            [],
            requireAtLeastOne: true);

    private static void ValidateReplacementLineReferences(Sale sale, List<ReplaceSaleProductDTO> requests)
    {
        var duplicatedLineId = requests
            .Where(request => request.SaleProductId.HasValue)
            .GroupBy(request => request.SaleProductId!.Value)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatedLineId is not null)
            throw new AppBadRequestException($"La linea de venta con id {duplicatedLineId.Key} está repetida.");

        foreach (var request in requests.Where(request => request.SaleProductId.HasValue))
        {
            var saleProductId = request.SaleProductId!.Value;
            var line = sale.Products.SingleOrDefault(product => product.Id == saleProductId)
                ?? throw new AppNotFoundException($"La linea de venta con id {saleProductId} no existe para la venta indicada.");
            if (line.ProductId != request.ProductId)
                throw new AppBadRequestException("No se puede cambiar la variante de una linea existente; elimine la linea y agregue otra.");
        }
    }

    private async Task ValidateReplacementPostSaleReferencesAsync(
        Sale sale,
        List<ReplaceSaleProductDTO> requests)
    {
        var saleProductIds = sale.Products.Select(product => product.Id).ToList();
        var returnItems = await _context.SaleReturnItems
            .AsNoTracking()
            .Include(item => item.SaleReturn)
            .Where(item => saleProductIds.Contains(item.OriginalSaleProductId))
            .ToListAsync();
        var exchangeItems = await _context.ExchangeReturnItems
            .AsNoTracking()
            .Include(item => item.SaleExchange)
            .Where(item => saleProductIds.Contains(item.OriginalSaleProductId))
            .ToListAsync();

        var requestsByLineId = requests
            .Where(request => request.SaleProductId.HasValue)
            .ToDictionary(request => request.SaleProductId!.Value);

        // Incluso una operación cancelada conserva la referencia histórica y evita eliminar la línea.
        var removedReferencedLineId = returnItems.Select(item => item.OriginalSaleProductId)
            .Concat(exchangeItems.Select(item => item.OriginalSaleProductId))
            .FirstOrDefault(lineId => !requestsByLineId.ContainsKey(lineId));
        if (removedReferencedLineId != 0)
            throw new AppBadRequestException(
                $"No se puede eliminar la linea de venta con id {removedReferencedLineId} porque tiene devoluciones o cambios asociados.");

        foreach (var (lineId, request) in requestsByLineId)
        {
            var activeReturnItems = returnItems.Where(item =>
                item.OriginalSaleProductId == lineId &&
                item.SaleReturn!.StatusId != (int)SaleReturnStatusOption.Cancelled);
            var activeExchangeItems = exchangeItems.Where(item =>
                item.OriginalSaleProductId == lineId &&
                item.SaleExchange!.StatusId != (int)SaleExchangeStatusOption.Cancelled);

            var referencedQuantity = activeReturnItems.Sum(item => item.Quantity) +
                                     activeExchangeItems.Sum(item => item.Quantity);
            if (request.Quantity < referencedQuantity)
                throw new AppBadRequestException(
                    $"La cantidad de la linea de venta con id {lineId} no puede ser menor que las {referencedQuantity} unidades incluidas en devoluciones o cambios activos.");

            var highestRecognizedUnitAmount = activeReturnItems.Select(item => item.RecognizedUnitAmount)
                .Concat(activeExchangeItems.Select(item => item.RecognizedUnitAmount))
                .DefaultIfEmpty()
                .Max();
            var line = sale.Products.Single(product => product.Id == lineId);
            var requestedFinalUnitPrice = Math.Round(line.OriginalUnitPrice - request.DiscountAmount, 2);
            if (requestedFinalUnitPrice < highestRecognizedUnitAmount)
                throw new AppBadRequestException(
                    $"El precio final de la linea de venta con id {lineId} no puede ser menor que el monto unitario ya reconocido en una devolución o cambio activo.");
        }
    }

    private async Task ValidateOptionalReferencesAsync(int? clientId)
    {
        if (clientId.HasValue && !await _context.Clients.AnyAsync(client => client.Id == clientId.Value))
            throw new AppNotFoundException($"La clienta con id '{clientId.Value}' no existe.");
    }

    private async Task ValidateRequestedSaleStatusAsync(int? saleStatusId)
    {
        if (!saleStatusId.HasValue) return;

        var status = (SaleStatusOption)saleStatusId.Value;
        if (status is not (SaleStatusOption.Pending or SaleStatusOption.Reserved or SaleStatusOption.ReadyForDelivery) ||
            !await _context.SaleStatuses.AnyAsync(saleStatus => saleStatus.Id == saleStatusId.Value))
        {
            throw new AppBadRequestException("El estado inicial de la venta no es valido.");
        }
    }

    private async Task ValidateProductRequestAsync(
        List<CreateSaleProductDTO> products,
        List<CreateSaleSelectionProductDTO> selectionProducts,
        bool requireAtLeastOne)
    {
        if (requireAtLeastOne && products.Count == 0 && selectionProducts.Count == 0)
            throw new AppBadRequestException("Debe enviar al menos un producto vendido o una prenda para seleccion.");

        foreach (var product in products)
        {
            if (product.Quantity <= 0) throw new AppBadRequestException("La cantidad de cada producto debe ser mayor que cero.");
            if (product.DiscountAmount < 0) throw new AppBadRequestException("El descuento no puede ser negativo.");

            var hasDiscount = product.DiscountAmount > 0;
            var discountSource = (DiscountSourceOption)product.DiscountSourceId;

            if (hasDiscount && discountSource == DiscountSourceOption.None)
                throw new AppBadRequestException("Debe indicar la fuente cuando aplica un descuento.");
            if (discountSource == DiscountSourceOption.Manual && product.DiscountCampaignId.HasValue)
                throw new AppBadRequestException("Un descuento manual no puede tener una campaña de descuento asociada.");
            if (!hasDiscount && (discountSource != DiscountSourceOption.None || product.DiscountCampaignId.HasValue))
                throw new AppBadRequestException("Cuando no hay descuento, la fuente debe ser None y no se debe indicar una campaña de descuento.");
            if (!await _context.DiscountSources.AnyAsync(source => source.Id == product.DiscountSourceId))
                throw new AppNotFoundException($"La fuente de descuento con id {product.DiscountSourceId} no existe.");
            if (product.DiscountCampaignId.HasValue && !await _context.DiscountCampaigns.AnyAsync(campaign => campaign.Id == product.DiscountCampaignId.Value))
                throw new AppNotFoundException($"La campana de descuento con id {product.DiscountCampaignId.Value} no existe.");
        }

        await ValidateSelectionProductRequestAsync(selectionProducts);
    }

    private async Task ValidateSaleProductRequestAsync(CreateSaleProductDTO product)
        => await ValidateProductRequestAsync([product], [], requireAtLeastOne: true);

    private static Task ValidateSelectionProductRequestAsync(List<CreateSaleSelectionProductDTO> selectionProducts)
    {
        foreach (var product in selectionProducts)
        {
            if (product.ProductId <= 0) throw new AppBadRequestException("Producto es obligatorio para seleccion.");
            if (product.Quantity <= 0) throw new AppBadRequestException("La cantidad de cada prenda para seleccion debe ser mayor que cero.");
        }

        return Task.CompletedTask;
    }

    private async Task<List<Product>> LoadSaleProductsAsync(
        List<CreateSaleProductDTO> productRequests,
        List<CreateSaleSelectionProductDTO> selectionProducts)
    {
        var productIds = productRequests.Select(product => product.ProductId)
            .Concat(selectionProducts.Select(product => product.ProductId))
            .Distinct().ToList();
        var products = await LoadProductsByIdsAsync(productIds);

        var requestedQuantities = productRequests.Select(product => new { product.ProductId, product.Quantity })
            .Concat(selectionProducts.GroupBy(product => product.ProductId).Select(group => new { ProductId = group.Key, Quantity = group.Sum(product => product.Quantity) }))
            .GroupBy(product => product.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(product => product.Quantity));
        foreach (var product in products)
        {
            if (product.AvailableQuantity < requestedQuantities[product.Id])
                throw new AppBadRequestException($"El producto con id {product.Id} no tiene stock disponible suficiente.");
        }

        return products;
    }

    private async Task<List<Product>> LoadProductsByIdsAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        var products = await _context.Products.Where(product => ids.Contains(product.Id)).ToListAsync();
        var missingProductId = ids.FirstOrDefault(id => products.All(product => product.Id != id));
        if (missingProductId != 0) throw new AppNotFoundException($"El producto con id {missingProductId} no existe.");
        return products;
    }

    private static List<SaleProduct> CreateSaleProducts(List<CreateSaleProductDTO> requests, List<Product> products)
    {
        return requests.Select(request =>
        {
            var product = products.First(item => item.Id == request.ProductId);
            if (request.DiscountAmount > product.SalePrice) throw new AppBadRequestException("El descuento no puede ser mayor que el precio de venta.");

            var finalUnitPrice = Math.Round(product.SalePrice - request.DiscountAmount, 2);
            var lineTotal = Math.Round(finalUnitPrice * request.Quantity, 2);
            var totalCostAtSale = Math.Round(product.UnitCostNio * request.Quantity, 6);
            return new SaleProduct
            {
                ProductId = product.Id,
                Product = product,
                Quantity = request.Quantity,
                UnitCostAtSale = product.UnitCostNio,
                OriginalUnitPrice = product.SalePrice,
                DiscountSourceId = request.DiscountSourceId,
                DiscountCampaignId = request.DiscountCampaignId,
                DiscountAmount = request.DiscountAmount,
                FinalUnitPrice = finalUnitPrice,
                LineTotal = lineTotal,
                TotalCostAtSale = totalCostAtSale,
                GrossProfit = lineTotal - totalCostAtSale
            };
        }).ToList();
    }

    private static void UpdateExistingSaleProduct(SaleProduct line, CreateSaleProductDTO request)
    {
        if (request.DiscountAmount > line.OriginalUnitPrice)
            throw new AppBadRequestException("El descuento no puede ser mayor que el precio original de la linea.");

        line.Quantity = request.Quantity;
        line.DiscountSourceId = request.DiscountSourceId;
        line.DiscountCampaignId = request.DiscountCampaignId;
        line.DiscountAmount = request.DiscountAmount;
        line.FinalUnitPrice = Math.Round(line.OriginalUnitPrice - request.DiscountAmount, 2);
        line.LineTotal = Math.Round(line.FinalUnitPrice * request.Quantity, 2);
        line.TotalCostAtSale = Math.Round(line.UnitCostAtSale * request.Quantity, 6);
        line.GrossProfit = line.LineTotal - line.TotalCostAtSale;
    }

    /// <summary>
    /// Valida que el inventario disponible pueda soportar el estado final solicitado para la venta.
    /// Si la venta todavía no ha afectado inventario, valida las cantidades completas; si ya lo
    /// afectó, valida únicamente el cambio neto después de considerar las unidades que se liberarán.
    /// </summary>
    private static void ValidateReplacementInventoryAvailability(
        InventoryStockBucketOption? inventoryBucket,
        List<Product> requestedProducts,
        List<(
            SaleProduct Line,
            ReplaceSaleProductDTO Request,
            int CurrentCommittedQuantity,
            int TargetCommittedQuantity)> requestedLineStates,
        List<SaleProduct> removedLines,
        IReadOnlyCollection<InventoryMovement> inventoryMovements)
    {
        // En estos estados todavía no existe un compromiso de inventario asociado a la venta.
        // Por eso debe estar disponible la cantidad final completa solicitada por producto.
        if (!inventoryBucket.HasValue)
        {
            var requestedQuantities = requestedLineStates
                .GroupBy(state => state.Request.ProductId)
                .ToDictionary(group => group.Key, group => group.Sum(state => state.Request.Quantity));
            foreach (var product in requestedProducts)
            {
                if (product.AvailableQuantity < requestedQuantities[product.Id])
                    throw new AppBadRequestException($"El producto con id {product.Id} no tiene stock disponible suficiente.");
            }

            return;
        }

        // Cuando la venta ya comprometió inventario, solo interesa la diferencia respecto a la cantidad
        // neta registrada. También se incluyen los productos eliminados porque liberarán unidades.
        var products = requestedProducts
            .Concat(removedLines.Select(line => line.Product!))
            .DistinctBy(product => product.Id);
        foreach (var product in products)
        {
            // Unidades que volverán a Available por reducir una línea o eliminarla completamente.
            var releasedQuantity = requestedLineStates
                    .Where(state => state.Line.ProductId == product.Id && state.CurrentCommittedQuantity > state.TargetCommittedQuantity)
                    .Sum(state => state.CurrentCommittedQuantity - state.TargetCommittedQuantity) +
                removedLines
                    .Where(line => line.ProductId == product.Id)
                    .Sum(line => CalculateQuantityInBucket(line.Id, inventoryMovements, inventoryBucket.Value));

            // Unidades adicionales que deben salir por aumentar una línea o agregar un producto.
            var requiredQuantity = requestedLineStates
                .Where(state => state.Line.ProductId == product.Id && state.TargetCommittedQuantity > state.CurrentCommittedQuantity)
                .Sum(state => state.TargetCommittedQuantity - state.CurrentCommittedQuantity);

            // Las unidades liberadas en esta misma operación pueden reutilizarse inmediatamente.
            if (product.AvailableQuantity + releasedQuantity < requiredQuantity)
                throw new AppBadRequestException($"El producto con id {product.Id} no tiene stock disponible suficiente.");
        }
    }

    private static List<ProductHold> CreateSelectionHolds(List<CreateSaleSelectionProductDTO> requests, List<Product> products)
        => requests.Select(request => new ProductHold
        {
            ProductId = request.ProductId,
            Product = products.Single(product => product.Id == request.ProductId),
            Quantity = request.Quantity,
            HoldReason = "SentForSelection",
            Comments = request.Comments.NormalizeOptional(),
            ProductHoldStatusId = (int)ProductHoldStatusOption.Active
        }).ToList();

    private void ApplySelectionHoldsInventory(IEnumerable<ProductHold> holds)
    {
        foreach (var hold in holds)
        {
            var product = hold.Product!;
            var movement = _inventoryService.Move(
                product,
                InventoryStockBucketOption.Available,
                InventoryStockBucketOption.Unavailable,
                hold.Quantity,
                InventoryMovementTypeOption.SelectionSent,
                hold.HoldDate,
                "Prenda enviada para seleccion.");
            movement.ProductHold = hold;
        }
    }
    /// <summary>
    /// Calculates the subtotal, total discount, and total for a sale based on its products.
    /// </summary>
    /// <param name="products">The list of sale products.</param>
    /// <returns>The calculated sale totals.</returns>
    private static SaleTotals CalculateSaleTotals(List<SaleProduct> products)
    {
        return new SaleTotals(
            Math.Round(products.Sum(product => product.OriginalUnitPrice * product.Quantity), 2),
            Math.Round(products.Sum(product => product.DiscountAmount * product.Quantity), 2),
            Math.Round(products.Sum(product => product.LineTotal), 2));
    }

    private static void RecalculateSaleTotals(Sale sale)
    {
        var totals = CalculateSaleTotals(sale.Products);
        sale.Subtotal = totals.Subtotal;
        sale.TotalDiscount = totals.TotalDiscount;
        sale.Total = totals.Total;
        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(
            sale.Total,
            SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements));
    }

    private static void CompleteInStoreSaleWhenPaid(Sale sale)
    {
        if (sale.SaleChannelId == (int)SaleChannelOption.InStoreSale &&
            sale.SalePaymentStatusId == (int)SalePaymentStatusOption.Paid)
        {
            sale.SaleStatusId = (int)SaleStatusOption.Completed;
        }
    }

    private async Task SynchronizeInStoreSaleCompletionAsync(int saleId)
    {
        var sale = await GetSaleWithDetailsAsync(saleId, asNoTracking: false);
        if (sale.SaleChannelId != (int)SaleChannelOption.InStoreSale ||
            sale.SaleStatusId == (int)SaleStatusOption.Cancelled)
        {
            return;
        }

        if (sale.SalePaymentStatusId == (int)SalePaymentStatusOption.Paid)
        {
            if (sale.SaleStatusId == (int)SaleStatusOption.Completed) return;

            var currentBucket = GetInventoryBucketForSaleStatus(sale.SaleStatusId);
            if (currentBucket == InventoryStockBucketOption.Reserved)
            {
                await TransitionCurrentSaleInventoryAsync(
                    sale,
                    InventoryStockBucketOption.Reserved,
                    InventoryStockBucketOption.OutOfInventory,
                    InventoryMovementTypeOption.ReservationConvertedToSale,
                    "Reserva convertida en venta local completada.");
            }
            else if (!currentBucket.HasValue)
            {
                ApplyInventoryForSaleStatus(sale, InventoryStockBucketOption.OutOfInventory);
            }

            sale.SaleStatusId = (int)SaleStatusOption.Completed;
        }
        else if (sale.SaleStatusId == (int)SaleStatusOption.Completed)
        {
            await TransitionCurrentSaleInventoryAsync(
                sale,
                InventoryStockBucketOption.OutOfInventory,
                InventoryStockBucketOption.Reserved,
                InventoryMovementTypeOption.ReservationCreated,
                "Venta local reabierta; productos reservados.");
            sale.SaleStatusId = (int)SaleStatusOption.Reserved;
        }

        await _context.SaveChangesAsync();
    }

    private static InventoryStockBucketOption? GetInventoryBucketForSaleStatus(int saleStatusId)
    {
        // El estado de la venta determina dónde debe vivir físicamente su compromiso de inventario.
        var status = (SaleStatusOption)saleStatusId;
        return status switch
        {
            SaleStatusOption.Reserved or SaleStatusOption.ReadyForDelivery => InventoryStockBucketOption.Reserved,
            SaleStatusOption.SentForDelivery or SaleStatusOption.Completed => InventoryStockBucketOption.OutOfInventory,
            _ => null
        };
    }

    private void ApplyInventoryForSaleStatus(Sale sale, InventoryStockBucketOption? targetBucket = null)
    {
        targetBucket ??= GetInventoryBucketForSaleStatus(sale.SaleStatusId);
        if (!targetBucket.HasValue) return;

        foreach (var saleProduct in sale.Products)
        {
            var movement = _inventoryService.Move(
                saleProduct.Product!,
                InventoryStockBucketOption.Available,
                targetBucket.Value,
                saleProduct.Quantity,
                GetCommitmentMovementType(targetBucket.Value),
                sale.SaleDate,
                targetBucket == InventoryStockBucketOption.Reserved
                    ? "Productos reservados para la venta."
                    : "Venta registrada.");
            movement.SaleProduct = saleProduct;
        }
    }

    private async Task TransitionCurrentSaleInventoryAsync(
        Sale sale,
        InventoryStockBucketOption source,
        InventoryStockBucketOption destination,
        InventoryMovementTypeOption movementType,
        string comments)
    {
        var saleProductIds = sale.Products.Select(product => product.Id).ToList();
        var inventoryMovements = await _context.InventoryMovements
            .Where(movement => movement.SaleProductId.HasValue && saleProductIds.Contains(movement.SaleProductId.Value))
            .ToListAsync();

        foreach (var saleProduct in sale.Products)
        {
            // Se mueve el saldo real del origen, no la cantidad histórica de la línea.
            var quantity = CalculateQuantityInBucket(saleProduct.Id, inventoryMovements, source);
            if (quantity == 0) continue;

            var movement = _inventoryService.Move(
                saleProduct.Product!,
                source,
                destination,
                quantity,
                movementType,
                DateTime.UtcNow,
                comments);
            movement.SaleProduct = saleProduct;
        }
    }

    private void RevertInventoryForCancelledSale(
        Sale sale,
        IReadOnlyCollection<InventoryMovement> inventoryMovements)
    {
        foreach (var saleProduct in sale.Products)
        {
            var product = saleProduct.Product!;
            foreach (var bucket in new[]
                     {
                         InventoryStockBucketOption.Reserved,
                         InventoryStockBucketOption.OutOfInventory
                     })
            {
                var quantityToRestore = CalculateQuantityInBucket(saleProduct.Id, inventoryMovements, bucket);
                if (quantityToRestore == 0) continue;

                var movement = _inventoryService.Move(
                    product,
                    bucket,
                    InventoryStockBucketOption.Available,
                    quantityToRestore,
                    GetReleaseMovementType(bucket),
                    DateTime.UtcNow,
                    "Venta cancelada.");
                movement.SaleProduct = saleProduct;
            }
        }
    }

    private static int CalculateQuantityInBucket(
        int saleProductId,
        IEnumerable<InventoryMovement> inventoryMovements,
        InventoryStockBucketOption bucket)
    {
        var quantity = inventoryMovements
            .Where(movement => movement.SaleProductId == saleProductId)
            .Sum(movement =>
                movement.ToStockBucketId == (int)bucket
                    ? movement.Quantity
                    : movement.FromStockBucketId == (int)bucket
                        ? -movement.Quantity
                        : 0);

        if (quantity < 0)
            throw new AppBadRequestException(
                $"Los movimientos de la linea de venta con id {saleProductId} tienen una cantidad neta invalida en {bucket}.");

        return quantity;
    }

    private static int CalculateReceivedReturnQuantity(
        int saleProductId,
        IEnumerable<InventoryMovement> inventoryMovements)
        // Solo estos movimientos prueban que la unidad ya regresó físicamente desde la venta.
        => inventoryMovements
            .Where(movement =>
                movement.SaleProductId == saleProductId &&
                movement.FromStockBucketId == (int)InventoryStockBucketOption.OutOfInventory &&
                movement.InventoryMovementTypeId is
                    (int)InventoryMovementTypeOption.CustomerReturn or
                    (int)InventoryMovementTypeOption.ExchangeReturnReceivedByAgency)
            .Sum(movement => movement.Quantity);

    private static InventoryMovementTypeOption GetCommitmentMovementType(InventoryStockBucketOption bucket)
        => bucket == InventoryStockBucketOption.Reserved
            ? InventoryMovementTypeOption.ReservationCreated
            : InventoryMovementTypeOption.Sale;

    private static InventoryMovementTypeOption GetReleaseMovementType(InventoryStockBucketOption bucket)
        => bucket == InventoryStockBucketOption.Reserved
            ? InventoryMovementTypeOption.ReservationReleased
            : InventoryMovementTypeOption.SaleCancelled;

    private void ReleaseActiveSelectionHoldsForCancelledSale(Sale sale)
    {
        foreach (var hold in sale.ProductHolds.Where(hold => hold.ProductHoldStatusId == (int)ProductHoldStatusOption.Active))
        {
            var product = hold.Product!;
            hold.ProductHoldStatusId = (int)ProductHoldStatusOption.NotSelected;
            hold.ResolvedAt = DateTime.UtcNow;
            var movement = _inventoryService.Move(
                product,
                InventoryStockBucketOption.Unavailable,
                InventoryStockBucketOption.Available,
                hold.Quantity,
                InventoryMovementTypeOption.SelectionReturned,
                DateTime.UtcNow,
                "Seleccion liberada por cancelacion de venta.");
            movement.ProductHold = hold;
        }
    }

    private async Task SyncInventoryMovementDatesAsync(Sale sale, DateTime previousSaleDate)
    {
        if (!GetInventoryBucketForSaleStatus(sale.SaleStatusId).HasValue) return;

        var saleProductIds = sale.Products.Select(product => product.Id).ToList();
        // Una corrección de fecha pertenece al movimiento original de la venta o reserva. Las fechas
        // operativas posteriores (envíos, devoluciones y cambios) deben conservar el momento real.
        var originalMovementTypes = new[]
        {
            (int)InventoryMovementTypeOption.Sale,
            (int)InventoryMovementTypeOption.ReservationCreated
        };
        var movements = await _context.InventoryMovements
            .Where(movement =>
                movement.SaleProductId.HasValue &&
                saleProductIds.Contains(movement.SaleProductId.Value) &&
                originalMovementTypes.Contains(movement.InventoryMovementTypeId) &&
                movement.MovementDate == previousSaleDate)
            .ToListAsync();
        foreach (var movement in movements) movement.MovementDate = sale.SaleDate;
    }

    private static void EnsureSaleHeaderCanBePatched(Sale sale)
    {
        if (sale.SaleStatusId == (int)SaleStatusOption.Cancelled)
            throw new AppBadRequestException("No se puede actualizar la cabecera de una venta cancelada.");
    }

    private static void EnsureSaleProductsCanBeReplaced(Sale sale)
    {
        if (sale.SaleStatusId is (int)SaleStatusOption.Cancelled or (int)SaleStatusOption.SentForDelivery or (int)SaleStatusOption.Completed)
            throw new AppBadRequestException("No se pueden reemplazar los productos de una venta cancelada enviada o completada.");
    }

    private static void EnsureSaleCanBeCancelled(Sale sale)
    {
        if (sale.SaleStatusId == (int)SaleStatusOption.Cancelled)
            throw new AppBadRequestException("La venta ya esta cancelada.");
        if (sale.SaleStatusId == (int)SaleStatusOption.Completed)
            throw new AppBadRequestException("No se puede cancelar una venta completada.");
        if (sale.Deliveries.Any(delivery => delivery.DeliveryStatusId is
            (int)DeliveryStatusCode.Sent or (int)DeliveryStatusCode.DeliveredPendingSelection))
            throw new AppBadRequestException("No se puede cancelar una venta con un envio enviado a la agencia.");
    }

    private static bool HasOutstandingPayments(Sale sale)
        => sale.PaymentMovements.Sum(payment =>
            (payment.MovementDirectionId == (int)MovementDirectionOptions.Out ? -1 : 1) *
            (payment.ProductAmount + payment.ShippingAmount)) != 0;

    private static void NormalizeSaleFields(CreateSaleDTO saleDTO)
    {
        saleDTO.Comments = saleDTO.Comments.NormalizeOptional();
        saleDTO.Products ??= [];
        saleDTO.SelectionProducts ??= [];
        saleDTO.PaymentMovements ??= [];
    }

    private static IQueryable<Sale> ApplySaleFilters(IQueryable<Sale> salesQuery, SaleQueryDTO query)
    {
        if (query.SaleStatusId.HasValue)
            salesQuery = salesQuery.Where(sale => sale.SaleStatusId == query.SaleStatusId.Value);
        if (query.SalePaymentStatusId.HasValue)
            salesQuery = salesQuery.Where(sale => sale.SalePaymentStatusId == query.SalePaymentStatusId.Value);
        if (query.SaleChannelId.HasValue)
            salesQuery = salesQuery.Where(sale => sale.SaleChannelId == query.SaleChannelId.Value);
        if (query.ClientId.HasValue)
            salesQuery = salesQuery.Where(sale => sale.ClientId == query.ClientId.Value);
        if (query.StartDate.HasValue)
            salesQuery = salesQuery.Where(sale => sale.SaleDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            salesQuery = salesQuery.Where(sale => sale.SaleDate <= query.EndDate.Value);

        return salesQuery;
    }

    private static void NormalizePagination(SaleQueryDTO query)
    {
        query.Page = Math.Max(query.Page, 1);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);
    }

    private static void NormalizeSaleFields(PatchSaleHeaderDTO saleDTO)
    {
        if (saleDTO.HasComments) saleDTO.Comments = saleDTO.Comments.NormalizeOptional();
    }

    private static void NormalizeSaleFields(ReplaceSaleProductsDTO saleDTO) => saleDTO.Products ??= [];

    private string ResolveUserId() => _currentUserService.UserId ?? "system";

    private static SaleDTO MapSaleDTO(Sale sale)
    {
        return new SaleDTO
        {
            Id = sale.Id,
            SaleDate = sale.SaleDate,
            SaleChannelId = sale.SaleChannelId,
            SaleChannelName = sale.SaleChannel?.Name,
            SaleStatusId = sale.SaleStatusId,
            SaleStatusName = sale.SaleStatus?.Name,
            SalePaymentStatusId = sale.SalePaymentStatusId,
            SalePaymentStatusName = sale.SalePaymentStatus?.Name,
            UserId = sale.UserId,
            Subtotal = sale.Subtotal,
            TotalDiscount = sale.TotalDiscount,
            Total = sale.Total,
            Comments = sale.Comments,
            ClientId = sale.ClientId,
            ClientName = sale.Client?.Name,
            Products = sale.Products.Select(product => new SaleProductDTO
            {
                Id = product.Id,
                ProductId = product.ProductId,
                Quantity = product.Quantity,
                UnitCostAtSale = product.UnitCostAtSale,
                OriginalUnitPrice = product.OriginalUnitPrice,
                DiscountSourceId = product.DiscountSourceId,
                DiscountSourceName = product.DiscountSource?.Name,
                DiscountCampaignId = product.DiscountCampaignId,
                DiscountAmount = product.DiscountAmount,
                FinalUnitPrice = product.FinalUnitPrice,
                LineTotal = product.LineTotal,
                TotalCostAtSale = product.TotalCostAtSale,
                GrossProfit = product.GrossProfit
            }).ToList(),
            SelectionHolds = sale.ProductHolds.Select(hold => new SaleSelectionHoldDTO
            {
                Id = hold.Id,
                ProductId = hold.ProductId,
                Quantity = hold.Quantity,
                ProductHoldStatusId = hold.ProductHoldStatusId,
                ProductHoldStatusName = hold.ProductHoldStatus?.Name,
                HoldDate = hold.HoldDate,
                ResolvedAt = hold.ResolvedAt,
                Comments = hold.Comments
            }).ToList(),
            PaymentMovements = sale.PaymentMovements.Select(payment => new SalePaymentMovementDTO
            {
                Id = payment.Id,
                MovementDate = payment.MovementDate,
                MovementDirectionId = payment.MovementDirectionId,
                PaymentMethodId = payment.PaymentMethodId,
                PaymentMethodName = payment.PaymentMethod?.Name,
                PaymentTerminalId = payment.PaymentTerminalId,
                PaymentTerminalName = payment.PaymentTerminal?.Name,
                ReversedSalePaymentMovementId = payment.ReversedSalePaymentMovementId,
                GrossAmount = payment.GrossAmount,
                ProductAmount = payment.ProductAmount,
                ShippingAmount = payment.ShippingAmount,
                SaleDeliveryId = payment.SaleDeliveryId,
                DeliveryAgencyReconciliationId = payment.DeliveryAgencyReconciliationId,
                CommissionPercentage = payment.CommissionPercentage,
                CommissionAmount = payment.CommissionAmount,
                IncomeTaxPercentage = payment.IncomeTaxPercentage,
                IncomeTaxAmount = payment.IncomeTaxAmount,
                NetReceivedAmount = payment.NetReceivedAmount,
                AmountReceivedNio = payment.AmountReceivedNio,
                AmountReceivedUsd = payment.AmountReceivedUsd,
                ChangeGivenNio = payment.ChangeGivenNio,
                ExchangeRate = payment.ExchangeRate,
                ExchangeDifferenceNio = payment.ExchangeDifferenceNio
            }).ToList(),
            Deliveries = sale.Deliveries.Select(delivery => new SaleDeliveryDTO
            {
                Id = delivery.Id,
                CreatedAt = delivery.CreatedAt,
                Code = delivery.Code,
                MunicipalityId = delivery.MunicipalityId,
                MunicipalityName = delivery.Municipality?.Name,
                DeliveryAgencyId = delivery.DeliveryAgencyId,
                DeliveryAgencyName = delivery.DeliveryAgency?.Name,
                DeliveryAgencyCanCollectCashOnDelivery = delivery.DeliveryAgency?.CanCollectCashOnDelivery ?? false,
                DeliveryStatusId = delivery.DeliveryStatusId,
                DeliveryStatusName = delivery.DeliveryStatus?.Name,
                ClientId = delivery.ClientId,
                AmountToCollect = delivery.AmountToCollect,
                AmountCollectedNio = delivery.AmountCollectedNio,
                AmountCollectedUsd = delivery.AmountCollectedUsd,
                ChangeGivenNio = delivery.ChangeGivenNio,
                CollectionExchangeRate = delivery.CollectionExchangeRate,
                ShippingChargedToClient = delivery.ShippingChargedToClient,
                ShippingPaidToAgency = delivery.ShippingPaidToAgency,
                DeliveryAddress = delivery.DeliveryAddress,
                Comments = delivery.Comments
            }).ToList()
        };
    }

    private sealed record SaleTotals(decimal Subtotal, decimal TotalDiscount, decimal Total);
}
