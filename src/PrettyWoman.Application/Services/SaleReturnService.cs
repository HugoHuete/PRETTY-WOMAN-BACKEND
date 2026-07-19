using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class SaleReturnService(
    IApplicationDbContext context,
    IInventoryService inventoryService) : ISaleReturnService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IInventoryService _inventoryService = inventoryService;

    public async Task<IEnumerable<SaleReturnDTO>> GetBySaleIdAsync(int saleId)
    {
        var returns = await _context.SaleReturns.AsNoTracking()
            .Include(item => item.Items)
            .Where(item => item.OriginalSaleId == saleId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
        return returns.Select(Map);
    }

    public async Task<int> CreateAsync(int saleId, CreateSaleReturnDTO request)
    {
        request.Items ??= [];
        if (request.Items.Count == 0)
            throw new AppBadRequestException("Una devolución debe incluir al menos una prenda.");
        if (!Enum.IsDefined(typeof(SaleReturnReasonOption), request.ReasonId) || !Enum.IsDefined(typeof(SaleReturnMethodOption), request.MethodId))
            throw new AppBadRequestException("El motivo o método de devolución no es válido.");
        if (request.ReturnShippingChargedToClient < 0 || request.ReturnShippingPaidToAgency < 0)
            throw new AppBadRequestException("Los montos de envío no pueden ser negativos.");
        if (request.ReasonId != (int)SaleReturnReasonOption.CustomerPreference && request.ReturnShippingChargedToClient > 0)
            throw new AppBadRequestException("La tienda asume el envío de retorno cuando la devolución es por defecto de producto o error de la tienda.");
        if (request.Items.Any(item => item.Quantity <= 0))
            throw new AppBadRequestException("La cantidad de cada prenda devuelta debe ser mayor que cero.");

        var method = (SaleReturnMethodOption)request.MethodId;
        if (method == SaleReturnMethodOption.DeliveryAgency && !request.DeliveryAgencyId.HasValue)
            throw new AppBadRequestException("La devolución por agencia requiere una agencia de envío.");

        if (method == SaleReturnMethodOption.InStore && (request.DeliveryAgencyId.HasValue || request.ReturnShippingChargedToClient > 0 || request.ReturnShippingPaidToAgency > 0))
            throw new AppBadRequestException("Una devolución en local no debe tener agencia ni costos de envío.");

        if (request.DeliveryAgencyId.HasValue && !await _context.DeliveryAgencies.AnyAsync(item => item.Id == request.DeliveryAgencyId.Value))
            throw new AppNotFoundException("La agencia de envío indicada no existe.");

        var sale = await _context.Sales
            .Include(item => item.Products)
            .Include(item => item.Deliveries)
            .FirstOrDefaultAsync(item => item.Id == saleId)
            ?? throw new AppNotFoundException($"La venta con id {saleId} no existe.");

        if (sale.SaleStatusId is not ((int)SaleStatusOption.SentForDelivery or (int)SaleStatusOption.Completed))
            throw new AppBadRequestException("Solo se pueden devolver productos de una venta enviada o completada.");

        await EnsureProductsWereMovedOutOfInventoryAsync(request.Items);
        var previouslyReturned = await GetPreviouslyReturnedQuantitiesAsync(saleId);
        foreach (var group in request.Items.GroupBy(item => item.OriginalSaleProductId))
        {
            var original = sale.Products.SingleOrDefault(item => item.Id == group.Key)
                ?? throw new AppBadRequestException("Cada prenda devuelta debe pertenecer a la venta original.");

            var quantity = group.Sum(item => item.Quantity);

            if (quantity <= 0 || previouslyReturned.GetValueOrDefault(group.Key) + quantity > original.Quantity)
                throw new AppBadRequestException("La cantidad a devolver excede la cantidad vendida disponible.");
                
            if (group.Any(item => item.RecognizedUnitAmount < 0 || item.RecognizedUnitAmount > original.FinalUnitPrice))
                throw new AppBadRequestException("El monto reconocido no puede ser negativo ni superar el precio final vendido.");
        }

        var result = new SaleReturn
        {
            OriginalSaleId = saleId,
            ReasonId = request.ReasonId,
            MethodId = request.MethodId,
            DeliveryAgencyId = request.DeliveryAgencyId,
            ReturnCode = request.ReturnCode.NormalizeOptional(),
            ReturnShippingChargedToClient = request.ReturnShippingChargedToClient,
            ReturnShippingPaidToAgency = request.ReturnShippingPaidToAgency,
            Comments = request.Comments.NormalizeOptional()
        };
        foreach (var item in request.Items)
        {
            var original = sale.Products.Single(product => product.Id == item.OriginalSaleProductId);
            result.Items.Add(new SaleReturnItem
            {
                OriginalSaleProductId = original.Id,
                ProductId = original.ProductId,
                Quantity = item.Quantity,
                RecognizedUnitAmount = item.RecognizedUnitAmount,
                OriginalUnitCost = original.UnitCostAtSale,
                Comments = item.Comments.NormalizeOptional()
            });
        }

        result.ProductRefundTotal = Math.Round(result.Items.Sum(item => item.Quantity * item.RecognizedUnitAmount), 2);
        var onlyOneProductWasSold = sale.Products.Sum(item => item.Quantity) == 1;
        result.OriginalShippingRefund = request.ReasonId != (int)SaleReturnReasonOption.CustomerPreference && onlyOneProductWasSold
            ? Math.Round(sale.Deliveries.Sum(item => item.ShippingChargedToClient), 2)
            : 0;
        result.RefundTotal = Math.Round(result.ProductRefundTotal + result.OriginalShippingRefund - result.ReturnShippingChargedToClient, 2);
        if (result.RefundTotal < 0)
            throw new AppBadRequestException("El envío de retorno no puede superar el monto a reembolsar.");

        await _context.SaleReturns.AddAsync(result);
        await _context.SaveChangesAsync();
        return result.Id;
    }

    public async Task RegisterAgencyPickupAsync(int saleId, int returnId, ProcessSaleReturnDTO request)
    {
        var result = await GetForUpdateAsync(saleId, returnId);
        if (result.MethodId != (int)SaleReturnMethodOption.DeliveryAgency || result.StatusId != (int)SaleReturnStatusOption.Requested)
            throw new AppBadRequestException("Solo una devolución por agencia solicitada puede marcarse como recogida.");
        await RegisterRefundAsync(result, request);
        result.PickedUpAt = request.ProcessedAt.NormalizeToUtc() ?? DateTime.UtcNow;
        result.StatusId = (int)SaleReturnStatusOption.PickedUpAndRefunded;
        result.Comments = request.Comments.NormalizeOptional() ?? result.Comments;
        await _context.SaveChangesAsync();
    }

    public async Task ReceiveAsync(int saleId, int returnId, ReceiveSaleReturnDTO request)
    {
        request.Items ??= [];
        var result = await GetForUpdateAsync(saleId, returnId);
        var canReceive = result.MethodId == (int)SaleReturnMethodOption.InStore
            ? result.StatusId == (int)SaleReturnStatusOption.Requested
            : result.StatusId == (int)SaleReturnStatusOption.PickedUpAndRefunded;
        if (!canReceive)
            throw new AppBadRequestException("La devolución no está en un estado que permita recibir sus prendas.");
        if (request.Items.Count != result.Items.Count || request.Items.Select(item => item.SaleReturnItemId).Distinct().Count() != result.Items.Count || request.Items.Any(item => result.Items.All(returnItem => returnItem.Id != item.SaleReturnItemId)))
            throw new AppBadRequestException("Debe indicar el estado de recepción de todas las prendas de la devolución.");

        var receivedAt = request.ReceivedAt.NormalizeToUtc() ?? DateTime.UtcNow;
        foreach (var receipt in request.Items)
        {
            var item = result.Items.Single(item => item.Id == receipt.SaleReturnItemId);
            ReceiveItem(item, receipt.IsDamaged, receivedAt, receipt.Comments.NormalizeOptional());
        }
        result.ReceivedAt = receivedAt;
        result.Comments = request.Comments.NormalizeOptional() ?? result.Comments;
        if (result.MethodId == (int)SaleReturnMethodOption.InStore)
        {
            // En local el dinero se entrega al recibir físicamente las prendas.
            // Sin monto a reembolsar no se registra un movimiento financiero ni se necesita método de pago.
            var paymentMethodId = result.RefundTotal > 0
                ? request.PaymentMethodId ?? throw new AppBadRequestException("Debe indicar el método usado para el reembolso en local.")
                : 0;
            await RegisterRefundAsync(result, new ProcessSaleReturnDTO { PaymentMethodId = paymentMethodId, ProcessedAt = receivedAt });
        }
        result.StatusId = (int)SaleReturnStatusOption.Completed;
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int saleId, int returnId)
    {
        var result = await GetForUpdateAsync(saleId, returnId);
        if (result.StatusId != (int)SaleReturnStatusOption.Requested)
            throw new AppBadRequestException("Solo se puede cancelar una devolución antes de recoger o recibir prendas.");
        result.StatusId = (int)SaleReturnStatusOption.Cancelled;
        await _context.SaveChangesAsync();
    }

    private async Task RegisterRefundAsync(SaleReturn result, ProcessSaleReturnDTO request)
    {
        if (result.RefundedAt.HasValue)
            return;
        if (result.RefundTotal > 0)
        {
            if (!await _context.PaymentMethods.AnyAsync(item => item.Id == request.PaymentMethodId))
                throw new AppBadRequestException("El método de reembolso no es válido o está deshabilitado.");
            var date = request.ProcessedAt.NormalizeToUtc() ?? DateTime.UtcNow;
            var rate = await GetExchangeRateAsync(date);
            result.RefundPaymentMethodId = request.PaymentMethodId;
            result.RefundedAt = date;
            await _context.FinancialMovements.AddAsync(new FinancialMovement
            {
                Description = $"Reembolso por devolución de venta #{result.OriginalSaleId}.",
                MovementDate = date,
                MovementDirectionId = (int)MovementDirectionOptions.Out,
                FinancialMovementTypeId = (int)FinancialMovementTypeOption.CustomerRefund,
                SaleReturn = result,
                Amount = result.RefundTotal,
                ExchangeRate = rate,
                Comments = result.Comments
            });
        }
        else result.RefundedAt = request.ProcessedAt.NormalizeToUtc() ?? DateTime.UtcNow;
    }

    private void ReceiveItem(SaleReturnItem item, bool isDamaged, DateTime receivedAt, string? comments)
    {
        var product = item.Product!;
        item.ReceivedAt = receivedAt;
        item.Comments = comments ?? item.Comments;

        if (isDamaged)
        {
            var issue = new ProductInventoryIssue
            {
                Product = product,
                ProductInventoryIssueTypeId = (int)ProductInventoryIssueTypeOption.Damaged,
                ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.Open,
                Quantity = item.Quantity,
                IssueDate = receivedAt,
                Comments = item.Comments
            };
            item.ProductInventoryIssue = issue;

            var movement = _inventoryService.Move(
                product,
                InventoryStockBucketOption.OutOfInventory,
                InventoryStockBucketOption.Unavailable,
                item.Quantity,
                InventoryMovementTypeOption.CustomerReturn,
                receivedAt,
                "Devolución recibida dañada; pendiente de resolución de inventario.");
            movement.SaleReturnItem = item;
            // Se conserva la línea original aunque la unidad entre a no disponible por daño.
            movement.SaleProductId = item.OriginalSaleProductId;
            movement.ProductInventoryIssue = issue;
            issue.InventoryMovements.Add(movement);
            return;
        }

        var availableMovement = _inventoryService.Move(
            product,
            InventoryStockBucketOption.OutOfInventory,
            InventoryStockBucketOption.Available,
            item.Quantity,
            InventoryMovementTypeOption.CustomerReturn,
            receivedAt,
            "Devolución recibida y disponible para venta.");
        availableMovement.SaleReturnItem = item;
        // La relación con la línea permite reconstruir cuánto inventario sigue comprometido.
        availableMovement.SaleProductId = item.OriginalSaleProductId;
    }

    private async Task<SaleReturn> GetForUpdateAsync(int saleId, int returnId)
        => await _context.SaleReturns
            .Include(item => item.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == returnId && item.OriginalSaleId == saleId)
            ?? throw new AppNotFoundException("La devolución no existe para la venta indicada.");

    private async Task<Dictionary<int, int>> GetPreviouslyReturnedQuantitiesAsync(int saleId)
    {
        var returns = await _context.SaleReturnItems.Include(item => item.SaleReturn)
            .Where(item => item.SaleReturn!.OriginalSaleId == saleId && item.SaleReturn.StatusId != (int)SaleReturnStatusOption.Cancelled)
            .GroupBy(item => item.OriginalSaleProductId).Select(group => new { Id = group.Key, Quantity = group.Sum(item => item.Quantity) }).ToListAsync();
        var exchanges = await _context.ExchangeReturnItems.Include(item => item.SaleExchange)
            .Where(item => item.SaleExchange!.OriginalSaleId == saleId && item.SaleExchange.StatusId != (int)SaleExchangeStatusOption.Cancelled)
            .GroupBy(item => item.OriginalSaleProductId).Select(group => new { Id = group.Key, Quantity = group.Sum(item => item.Quantity) }).ToListAsync();
        return returns.Concat(exchanges).GroupBy(item => item.Id).ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
    }

    private async Task EnsureProductsWereMovedOutOfInventoryAsync(IEnumerable<CreateSaleReturnItemDTO> items)
    {
        var ids = items.Select(item => item.OriginalSaleProductId).Distinct().ToList();
        var moved = await _context.InventoryMovements
            .Where(item =>
                ids.Contains(item.SaleProductId ?? 0) &&
                item.ToStockBucketId == (int)InventoryStockBucketOption.OutOfInventory &&
                (item.InventoryMovementTypeId == (int)InventoryMovementTypeOption.Sale ||
                 item.InventoryMovementTypeId == (int)InventoryMovementTypeOption.ReservationConvertedToSale ||
                 item.InventoryMovementTypeId == (int)InventoryMovementTypeOption.SelectionConvertedToSale))
            .Select(item => item.SaleProductId!.Value)
            .Distinct()
            .ToListAsync();
        if (moved.Count != ids.Count) throw new AppBadRequestException("Solo se pueden devolver prendas cuya venta ya descontó inventario.");
    }

    private async Task<decimal> GetExchangeRateAsync(DateTime date)
        => await _context.DollarExchangeRates.Where(item => item.Enabled && item.StartDate <= date).OrderByDescending(item => item.StartDate).Select(item => (decimal?)item.BankRate).FirstOrDefaultAsync()
            ?? throw new AppBadRequestException("No existe una tasa de cambio bancaria habilitada para la fecha del reembolso.");

    private static SaleReturnDTO Map(SaleReturn item) => new()
    {
        Id = item.Id, OriginalSaleId = item.OriginalSaleId, ReasonId = item.ReasonId, ReasonName = ((SaleReturnReasonOption)item.ReasonId).ToString(), MethodId = item.MethodId, MethodName = ((SaleReturnMethodOption)item.MethodId).ToString(), StatusId = item.StatusId, StatusName = ((SaleReturnStatusOption)item.StatusId).ToString(), DeliveryAgencyId = item.DeliveryAgencyId, ReturnCode = item.ReturnCode, ProductRefundTotal = item.ProductRefundTotal, ReturnShippingChargedToClient = item.ReturnShippingChargedToClient, ReturnShippingPaidToAgency = item.ReturnShippingPaidToAgency, OriginalShippingRefund = item.OriginalShippingRefund, RefundTotal = item.RefundTotal, RefundPaymentMethodId = item.RefundPaymentMethodId, PickedUpAt = item.PickedUpAt, ReceivedAt = item.ReceivedAt, Comments = item.Comments,
        Items = item.Items.Select(detail => new SaleReturnItemDTO { Id = detail.Id, OriginalSaleProductId = detail.OriginalSaleProductId, ProductId = detail.ProductId, Quantity = detail.Quantity, RecognizedUnitAmount = detail.RecognizedUnitAmount, OriginalUnitCost = detail.OriginalUnitCost, ProductInventoryIssueId = detail.ProductInventoryIssueId, ReceivedAt = detail.ReceivedAt, Comments = detail.Comments }).ToList()
    };
}
