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
    ISaleDeliveryService deliveryService) : ISaleService
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ISalePaymentMovementService _paymentMovementService = paymentMovementService;
    private readonly ISaleDeliveryService _deliveryService = deliveryService;

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

        // El inventario sale solo para estados que ya comprometen existencias.
        if (SaleAffectsInventory(sale.SaleStatusId))
        {
            ApplyInventoryOutput(sale);
        }

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
            sale.SaleDate = patchSaleHeaderDTO.SaleDate!.Value;
            await SyncInventoryMovementDatesAsync(sale);
        }

        if (patchSaleHeaderDTO.HasClientId) sale.ClientId = patchSaleHeaderDTO.ClientId;
        if (patchSaleHeaderDTO.HasComments) sale.Comments = patchSaleHeaderDTO.Comments;

        await _context.SaveChangesAsync();
    }

    public async Task ReplaceProductsAsync(int id, ReplaceSaleProductsDTO replaceSaleProductsDTO)
    {
        NormalizeSaleFields(replaceSaleProductsDTO);
        await ValidateSaleProductsReplacementAsync(replaceSaleProductsDTO);

        // Reemplazar lineas rehace sus totales e inventario, pero conserva pagos y envios existentes.
        var sale = await GetSaleWithDetailsAsync(id, asNoTracking: false);
        EnsureSaleProductsCanBeReplaced(sale);

        // Se revierte la salida anterior antes de reemplazar lineas para no duplicar movimientos.
        if (SaleAffectsInventory(sale.SaleStatusId))
        {
            RevertInventoryOutput(sale);
        }

        var saleProductIds = sale.Products.Select(product => product.Id).ToList();
        var inventoryMovements = await _context.InventoryMovements
            .Where(movement => movement.SaleProductId.HasValue && saleProductIds.Contains(movement.SaleProductId.Value))
            .ToListAsync();
        _context.InventoryMovements.RemoveRange(inventoryMovements);
        _context.SaleProducts.RemoveRange(sale.Products);

        var products = await LoadSaleProductsAsync(replaceSaleProductsDTO.Products, []);
        var saleProducts = CreateSaleProducts(replaceSaleProductsDTO.Products, products);
        // Los nuevos totales se validan contra pagos ya aplicados; un exceso queda como reembolso pendiente.
        var totals = CalculateSaleTotals(saleProducts);
        var paymentTotal = SalePaymentMovementRules.CalculateProductTotal(sale.PaymentMovements);
        SalePaymentMovementRules.EnsureAllowedProductTotal(sale.SaleChannelId, totals.Total, paymentTotal, allowOverpayment: true);

        sale.SalePaymentStatusId = SalePaymentMovementRules.ResolveStatus(totals.Total, paymentTotal);
        sale.Subtotal = totals.Subtotal;
        sale.TotalDiscount = totals.TotalDiscount;
        sale.Total = totals.Total;
        sale.Products = saleProducts;
        await _deliveryService.SyncActiveAmountToCollectAsync(sale.Id, sale.Total, sale.PaymentMovements);

        // El inventario sale solo para estados que ya comprometen existencias.
        if (SaleAffectsInventory(sale.SaleStatusId))
        {
            ApplyInventoryOutput(sale);
        }

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
            product.UnavailableQuantity -= hold.Quantity;
            product.InventoryMovements.Add(new InventoryMovement
            {
                Product = product,
                ProductHold = hold,
                SaleProduct = saleProduct,
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.SelectionConvertedToSale,
                FromStockBucketId = (int)InventoryStockBucketOption.Unavailable,
                ToStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                Quantity = hold.Quantity,
                MovementDate = DateTime.UtcNow,
                Comments = "Prenda de seleccion convertida a venta."
            });
        }
        else
        {
            hold.ProductHoldStatusId = (int)ProductHoldStatusOption.AwaitingReturn;
            product.UnavailableQuantity -= hold.Quantity;
            product.AvailableQuantity += hold.Quantity;
            product.InventoryMovements.Add(new InventoryMovement
            {
                Product = product,
                ProductHold = hold,
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.SelectionReturned,
                FromStockBucketId = (int)InventoryStockBucketOption.Unavailable,
                ToStockBucketId = (int)InventoryStockBucketOption.Available,
                Quantity = hold.Quantity,
                MovementDate = DateTime.UtcNow,
                Comments = "Prenda no seleccionada; pendiente de retorno fisico."
            });
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

        if (SaleAffectsInventory(sale.SaleStatusId))
        {
            RevertInventoryForCancelledSale(sale);
        }

        ReleaseActiveSelectionHoldsForCancelledSale(sale);

        foreach (var saleProduct in sale.Products)
        {
            saleProduct.SaleProductStatusId = (int)SaleProductStatusOption.Cancelled;
        }

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

    public async Task AdjustPaymentMovementsAsync(int saleId, AdjustSalePaymentMovementsDTO adjustment)
    {
        await _paymentMovementService.AdjustAsync(saleId, adjustment);
        await SynchronizeInStoreSaleCompletionAsync(saleId);
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
            .Include(sale => sale.Products).ThenInclude(saleProduct => saleProduct.SaleProductStatus)
            .Include(sale => sale.ProductHolds).ThenInclude(hold => hold.Product)
            .Include(sale => sale.ProductHolds).ThenInclude(hold => hold.ProductHoldStatus)
            .Include(sale => sale.PaymentMovements).ThenInclude(payment => payment.PaymentMethod)
            .Include(sale => sale.PaymentMovements).ThenInclude(payment => payment.PaymentTerminal)
            .Include(sale => sale.Deliveries)
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
        => await ValidateProductRequestAsync(saleDTO.Products, [], requireAtLeastOne: true);

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
        var products = await _context.Products.Where(product => productIds.Contains(product.Id)).ToListAsync();
        var missingProductId = productIds.FirstOrDefault(id => products.All(product => product.Id != id));
        if (missingProductId != 0) throw new AppNotFoundException($"El producto con id {missingProductId} no existe.");

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
                GrossProfit = lineTotal - totalCostAtSale,
                SaleProductStatusId = (int)SaleProductStatusOption.Completed
            };
        }).ToList();
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

    private static void ApplySelectionHoldsInventory(IEnumerable<ProductHold> holds)
    {
        foreach (var hold in holds)
        {
            var product = hold.Product!;
            product.AvailableQuantity -= hold.Quantity;
            product.UnavailableQuantity += hold.Quantity;
            product.InventoryMovements.Add(new InventoryMovement
            {
                Product = product,
                ProductHold = hold,
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.SelectionSent,
                FromStockBucketId = (int)InventoryStockBucketOption.Available,
                ToStockBucketId = (int)InventoryStockBucketOption.Unavailable,
                Quantity = hold.Quantity,
                MovementDate = hold.HoldDate,
                Comments = "Prenda enviada para seleccion."
            });
        }
    }

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

            var alreadyAffectsInventory = SaleAffectsInventory(sale.SaleStatusId);
            sale.SaleStatusId = (int)SaleStatusOption.Completed;
            if (!alreadyAffectsInventory) ApplyInventoryOutput(sale);
        }
        else if (sale.SaleStatusId == (int)SaleStatusOption.Completed)
        {
            // A refund or payment correction reopens the sale while keeping its inventory reserved.
            sale.SaleStatusId = (int)SaleStatusOption.Reserved;
        }

        await _context.SaveChangesAsync();
    }

    private static bool SaleAffectsInventory(int saleStatusId)
    {
        var status = (SaleStatusOption)saleStatusId;
        return status is SaleStatusOption.Reserved or SaleStatusOption.ReadyForDelivery or SaleStatusOption.SentForDelivery or SaleStatusOption.Completed;
    }

    private static void ApplyInventoryOutput(Sale sale)
    {
        foreach (var saleProduct in sale.Products)
        {
            saleProduct.Product!.AvailableQuantity -= saleProduct.Quantity;
            saleProduct.Product.InventoryMovements.Add(new InventoryMovement
            {
                Product = saleProduct.Product,
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.Sale,
                FromStockBucketId = (int)InventoryStockBucketOption.Available,
                ToStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                Quantity = saleProduct.Quantity,
                SaleProduct = saleProduct,
                MovementDate = sale.SaleDate,
                Comments = "Venta registrada."
            });
        }
    }

    private static void RevertInventoryOutput(Sale sale)
    {
        foreach (var saleProduct in sale.Products) saleProduct.Product!.AvailableQuantity += saleProduct.Quantity;
    }

    private static void RevertInventoryForCancelledSale(Sale sale)
    {
        foreach (var saleProduct in sale.Products)
        {
            var product = saleProduct.Product!;
            product.AvailableQuantity += saleProduct.Quantity;
            product.InventoryMovements.Add(new InventoryMovement
            {
                Product = product,
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.SaleCancelled,
                FromStockBucketId = (int)InventoryStockBucketOption.OutOfInventory,
                ToStockBucketId = (int)InventoryStockBucketOption.Available,
                Quantity = saleProduct.Quantity,
                SaleProduct = saleProduct,
                MovementDate = DateTime.UtcNow,
                Comments = "Venta cancelada."
            });
        }
    }

    private static void ReleaseActiveSelectionHoldsForCancelledSale(Sale sale)
    {
        foreach (var hold in sale.ProductHolds.Where(hold => hold.ProductHoldStatusId == (int)ProductHoldStatusOption.Active))
        {
            var product = hold.Product!;
            product.UnavailableQuantity -= hold.Quantity;
            product.AvailableQuantity += hold.Quantity;
            hold.ProductHoldStatusId = (int)ProductHoldStatusOption.NotSelected;
            hold.ResolvedAt = DateTime.UtcNow;
            product.InventoryMovements.Add(new InventoryMovement
            {
                Product = product,
                ProductHold = hold,
                InventoryMovementTypeId = (int)InventoryMovementTypeOption.SelectionReturned,
                FromStockBucketId = (int)InventoryStockBucketOption.Unavailable,
                ToStockBucketId = (int)InventoryStockBucketOption.Available,
                Quantity = hold.Quantity,
                MovementDate = DateTime.UtcNow,
                Comments = "Seleccion liberada por cancelacion de venta."
            });
        }
    }

    private async Task SyncInventoryMovementDatesAsync(Sale sale)
    {
        if (!SaleAffectsInventory(sale.SaleStatusId)) return;

        var saleProductIds = sale.Products.Select(product => product.Id).ToList();
        var movements = await _context.InventoryMovements
            .Where(movement => movement.SaleProductId.HasValue && saleProductIds.Contains(movement.SaleProductId.Value))
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
                GrossProfit = product.GrossProfit,
                SaleProductStatusId = product.SaleProductStatusId,
                SaleProductStatusName = product.SaleProductStatus?.Name
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
                NetReceivedAmount = payment.NetReceivedAmount
            }).ToList()
        };
    }

    private sealed record SaleTotals(decimal Subtotal, decimal TotalDiscount, decimal Total);
}
