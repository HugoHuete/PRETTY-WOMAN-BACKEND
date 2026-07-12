using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Calculations;
using PrettyWoman.Application.Common.Extensions;
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

    public async Task<IEnumerable<SaleDTO>> GetAllAsync()
    {
        var sales = await _context.Sales
            .AsNoTracking()
            .Include(sale => sale.SaleChannel)
            .Include(sale => sale.SaleStatus)
            .Include(sale => sale.SalePaymentStatus)
            .Include(sale => sale.Client)
.OrderByDescending(sale => sale.SaleDate)
            .ThenByDescending(sale => sale.Id)
            .ToListAsync();

        return sales.Select(MapSaleDTO).ToList();
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
        var products = await LoadSaleProductsAsync(createSaleDTO.Products);
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
            PaymentMovements = payments
        };

        // El inventario sale solo para estados que ya comprometen existencias.
        if (SaleAffectsInventory(sale.SaleStatusId))
        {
            ApplyInventoryOutput(sale);
        }

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

        var products = await LoadSaleProductsAsync(replaceSaleProductsDTO.Products);
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

    public Task<int> CreateDeliveryAsync(int saleId, CreateSaleDeliveryDTO delivery)
        => _deliveryService.CreateAsync(saleId, delivery);

    public Task UpdateDeliveryAsync(int saleId, int deliveryId, PatchSaleDeliveryDTO delivery)
        => _deliveryService.PatchAsync(saleId, deliveryId, delivery);

    public Task MarkDeliveryAsSentAsync(int saleId, int deliveryId)
        => _deliveryService.MarkAsSentAsync(saleId, deliveryId);

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

        foreach (var saleProduct in sale.Products)
        {
            saleProduct.SaleProductStatusId = (int)SaleProductStatusOption.Cancelled;
        }

        sale.SaleStatusId = (int)SaleStatusOption.Cancelled;
        await _context.SaveChangesAsync();
    }

    public Task<int> AddPaymentMovementAsync(int saleId, CreateSalePaymentMovementDTO paymentMovement)
        => _paymentMovementService.AddAsync(saleId, paymentMovement);

    public Task UpdatePaymentMovementAsync(int saleId, int paymentMovementId, UpdateSalePaymentMovementDTO paymentMovement)
        => _paymentMovementService.PatchAsync(saleId, paymentMovementId, paymentMovement);

    public Task<int> RefundPaymentMovementAsync(int saleId, int paymentMovementId, RefundSalePaymentMovementDTO refund)
        => _paymentMovementService.RefundAsync(saleId, paymentMovementId, refund);

    public Task AdjustPaymentMovementsAsync(int saleId, AdjustSalePaymentMovementsDTO adjustment)
        => _paymentMovementService.AdjustAsync(saleId, adjustment);

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
        await ValidateProductRequestAsync(saleDTO.Products);
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
        => await ValidateProductRequestAsync(saleDTO.Products);

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

    private async Task ValidateProductRequestAsync(List<CreateSaleProductDTO> products)
    {
        if (products.Count == 0) throw new AppBadRequestException("Debe enviar al menos un producto para la venta.");

        foreach (var product in products)
        {
            if (product.Quantity <= 0) throw new AppBadRequestException("La cantidad de cada producto debe ser mayor que cero.");
            if (product.DiscountAmount < 0) throw new AppBadRequestException("El descuento no puede ser negativo.");
            if (!await _context.DiscountSources.AnyAsync(source => source.Id == product.DiscountSourceId))
                throw new AppNotFoundException($"La fuente de descuento con id {product.DiscountSourceId} no existe.");
            if (product.DiscountCampaignId.HasValue && !await _context.DiscountCampaigns.AnyAsync(campaign => campaign.Id == product.DiscountCampaignId.Value))
                throw new AppNotFoundException($"La campana de descuento con id {product.DiscountCampaignId.Value} no existe.");
        }
    }

    private async Task<List<Product>> LoadSaleProductsAsync(List<CreateSaleProductDTO> productRequests)
    {
        var productIds = productRequests.Select(product => product.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(product => productIds.Contains(product.Id)).ToListAsync();
        var missingProductId = productIds.FirstOrDefault(id => products.All(product => product.Id != id));
        if (missingProductId != 0) throw new AppNotFoundException($"El producto con id {missingProductId} no existe.");

        var requestedQuantities = productRequests.GroupBy(product => product.ProductId).ToDictionary(group => group.Key, group => group.Sum(product => product.Quantity));
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

    private static SaleTotals CalculateSaleTotals(List<SaleProduct> products)
    {
        return new SaleTotals(
            Math.Round(products.Sum(product => product.OriginalUnitPrice * product.Quantity), 2),
            Math.Round(products.Sum(product => product.DiscountAmount * product.Quantity), 2),
            Math.Round(products.Sum(product => product.LineTotal), 2));
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
        if (sale.Deliveries.Any(delivery => delivery.DeliveryStatusId == (int)DeliveryStatusCode.Sent))
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
        saleDTO.PaymentMovements ??= [];
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
