using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Calculations;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class SaleDeliveryService(IApplicationDbContext context, ICurrentUserService currentUserService) : ISaleDeliveryService
{
    private const string ActiveDeliveryConstraintName = "ux_sale_deliveries_sale_id_active";
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<int> CreateAsync(int saleId, CreateSaleDeliveryDTO deliveryRequest)
    {
        Normalize(deliveryRequest);
        ValidateAmounts(deliveryRequest);

        var sale = await _context.Sales
            .Include(item => item.PaymentMovements)
                .ThenInclude(payment => payment.ReversalMovements)
            .FirstOrDefaultAsync(item => item.Id == saleId)
            ?? throw new AppNotFoundException($"La venta con id {saleId} no existe.");

        EnsureSaleCanHaveDelivery(sale);
        await EnsureNoActiveDeliveryAsync(saleId);

        var agency = await _context.DeliveryAgencies
            .FirstOrDefaultAsync(item => item.Id == deliveryRequest.DeliveryAgencyId && item.Enabled)
            ?? throw new AppNotFoundException($"La agencia de envio con id {deliveryRequest.DeliveryAgencyId} no existe o no esta habilitada.");

        if (!await _context.Municipalities.AnyAsync(item => item.Id == deliveryRequest.MunicipalityId))
        {
            throw new AppNotFoundException($"El municipio con id {deliveryRequest.MunicipalityId} no existe.");
        }

        var clientId = deliveryRequest.ClientId ?? sale.ClientId;
        string? clientAddress = null;
        if (clientId.HasValue)
        {
            var client = await _context.Clients
                .Where(item => item.Id == clientId.Value && !item.IsBlocked)
                .Select(item => new { item.Address })
                .FirstOrDefaultAsync();
            if (client is null)
            {
                throw new AppNotFoundException($"La clienta con id {clientId.Value} no existe.");
            }

            clientAddress = client.Address;
        }

        var paymentTotal = SalePaymentMovementRules.CalculateTotal(sale.PaymentMovements);
        var amountToCollect = CalculateAmountToCollect(sale.Total, paymentTotal, deliveryRequest.ShippingChargedToClient, agency);

        var delivery = new SaleDelivery
        {
            Code = deliveryRequest.Code,
            SaleId = saleId,
            MunicipalityId = deliveryRequest.MunicipalityId,
            DeliveryAgencyId = agency.Id,
            DeliveryStatusId = (int)DeliveryStatusCode.Pending,
            ClientId = clientId,
            AmountToCollect = amountToCollect,
            ShippingChargedToClient = deliveryRequest.ShippingChargedToClient,
            DeliveryAddress = deliveryRequest.DeliveryAddress ?? clientAddress,
            UserId = ResolveUserId(),
            Comments = deliveryRequest.Comments
        };

        sale.SaleStatusId = (int)SaleStatusOption.ReadyForDelivery;
        try
        {
            await _context.SaleDeliveries.AddAsync(delivery);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsActiveDeliveryUniqueViolation(ex))
        {
            throw new AppBadRequestException("La venta ya tiene un envio activo.");
        }

        return delivery.Id;
    }

    public async Task MarkAsSentAsync(int saleId, int deliveryId)
    {
        var delivery = await _context.SaleDeliveries
            .Include(item => item.Sale)
            .FirstOrDefaultAsync(item => item.Id == deliveryId && item.SaleId == saleId)
            ?? throw new AppNotFoundException($"El envio con id {deliveryId} no existe para la venta indicada.");

        if (delivery.DeliveryStatusId is (int)DeliveryStatusCode.Completed or (int)DeliveryStatusCode.Cancelled)
        {
            throw new AppBadRequestException("No se puede enviar un envio completado o cancelado.");
        }

        var sale = delivery.Sale!;
        EnsureSaleCanHaveDelivery(sale);
        sale.SaleStatusId = (int)SaleStatusOption.SentForDelivery;

        await _context.SaveChangesAsync();
    }

    public async Task PatchAsync(int saleId, int deliveryId, PatchSaleDeliveryDTO deliveryRequest)
    {
        Normalize(deliveryRequest);
        ValidateAmounts(deliveryRequest);

        var delivery = await _context.SaleDeliveries
            .Include(item => item.Sale)
                .ThenInclude(sale => sale!.PaymentMovements)
                    .ThenInclude(payment => payment.ReversalMovements)
            .Include(item => item.DeliveryAgency)
            .FirstOrDefaultAsync(item => item.Id == deliveryId && item.SaleId == saleId)
            ?? throw new AppNotFoundException($"El envio con id {deliveryId} no existe para la venta indicada.");

        EnsureDeliveryCanBeUpdated(delivery);

        var agency = delivery.DeliveryAgency!;
        if (deliveryRequest.HasDeliveryAgencyId)
        {
            agency = await _context.DeliveryAgencies
                .FirstOrDefaultAsync(item => item.Id == deliveryRequest.DeliveryAgencyId && item.Enabled)
                ?? throw new AppNotFoundException($"La agencia de envio con id {deliveryRequest.DeliveryAgencyId} no existe o no esta habilitada.");
        }

        if (deliveryRequest.HasMunicipalityId &&
            !await _context.Municipalities.AnyAsync(item => item.Id == deliveryRequest.MunicipalityId))
        {
            throw new AppNotFoundException($"El municipio con id {deliveryRequest.MunicipalityId} no existe.");
        }

        if (deliveryRequest.HasClientId && deliveryRequest.ClientId.HasValue &&
            !await _context.Clients.AnyAsync(item => item.Id == deliveryRequest.ClientId.Value && !item.IsBlocked))
        {
            throw new AppNotFoundException($"La clienta con id {deliveryRequest.ClientId.Value} no existe.");
        }

        var shippingChargedToClient = deliveryRequest.HasShippingChargedToClient
            ? deliveryRequest.ShippingChargedToClient!.Value
            : delivery.ShippingChargedToClient;
        var paymentTotal = SalePaymentMovementRules.CalculateTotal(delivery.Sale!.PaymentMovements);
        var amountToCollect = CalculateAmountToCollect(delivery.Sale.Total, paymentTotal, shippingChargedToClient, agency);

        if (deliveryRequest.HasClientId) delivery.ClientId = deliveryRequest.ClientId;
        if (deliveryRequest.HasDeliveryAddress) delivery.DeliveryAddress = deliveryRequest.DeliveryAddress;
        if (deliveryRequest.HasDeliveryAgencyId) delivery.DeliveryAgencyId = agency.Id;
        if (deliveryRequest.HasMunicipalityId) delivery.MunicipalityId = deliveryRequest.MunicipalityId!.Value;
        if (deliveryRequest.HasCode) delivery.Code = deliveryRequest.Code!;
        if (deliveryRequest.HasShippingChargedToClient) delivery.ShippingChargedToClient = shippingChargedToClient;
        delivery.AmountToCollect = amountToCollect;

        await _context.SaveChangesAsync();
    }

    public async Task SyncActiveAmountToCollectAsync(int saleId, decimal saleTotal, decimal paymentTotal)
    {
        var delivery = await GetActiveDeliveryAsync(saleId);
        if (delivery is null)
        {
            return;
        }

        delivery.AmountToCollect = CalculateAmountToCollect(
            saleTotal,
            paymentTotal,
            delivery.ShippingChargedToClient,
            delivery.DeliveryAgency!);
    }

    public async Task EnsureSaleChannelCanBeChangedAsync(int saleId, int saleChannelId)
    {
        if (saleChannelId != (int)SaleChannelOption.InStoreSale)
        {
            return;
        }

        if (await GetActiveDeliveryAsync(saleId) is not null)
        {
            throw new AppBadRequestException("Una venta con un envio activo no puede cambiarse a venta en local.");
        }
    }

    private async Task<SaleDelivery?> GetActiveDeliveryAsync(int saleId)
    {
        return await _context.SaleDeliveries
            .Include(item => item.DeliveryAgency)
            .FirstOrDefaultAsync(item =>
                item.SaleId == saleId &&
                item.DeliveryStatusId != (int)DeliveryStatusCode.Completed &&
                item.DeliveryStatusId != (int)DeliveryStatusCode.Cancelled);
    }

    private async Task EnsureNoActiveDeliveryAsync(int saleId)
    {
        if (await GetActiveDeliveryAsync(saleId) is not null)
        {
            throw new AppBadRequestException("La venta ya tiene un envio activo.");
        }
    }

    private static bool IsActiveDeliveryUniqueViolation(DbUpdateException exception)
    {
        var innerException = exception.InnerException;
        if (innerException is null || !string.Equals(innerException.GetType().Name, "PostgresException", StringComparison.Ordinal))
        {
            return false;
        }

        return innerException.Message.Contains(ActiveDeliveryConstraintName, StringComparison.Ordinal);
    }

    private static void EnsureSaleCanHaveDelivery(Sale sale)
    {
        if (sale.SaleChannelId == (int)SaleChannelOption.InStoreSale)
        {
            throw new AppBadRequestException("Las ventas en local no pueden tener envios.");
        }

        if (sale.SaleStatusId is (int)SaleStatusOption.Completed or (int)SaleStatusOption.Cancelled)
        {
            throw new AppBadRequestException("No se puede crear o enviar un envio para una venta completada o cancelada.");
        }
    }

    private static void EnsureDeliveryCanBeUpdated(SaleDelivery delivery)
    {
        if (delivery.DeliveryStatusId is (int)DeliveryStatusCode.Completed or (int)DeliveryStatusCode.Cancelled)
        {
            throw new AppBadRequestException("No se puede modificar un envio completado o cancelado.");
        }

        var sale = delivery.Sale!;
        EnsureSaleCanHaveDelivery(sale);
        if (sale.SaleStatusId == (int)SaleStatusOption.SentForDelivery)
        {
            throw new AppBadRequestException("No se puede modificar un envio despues de haber sido enviado a la agencia.");
        }
    }

    private static decimal CalculateAmountToCollect(decimal saleTotal, decimal paymentTotal, decimal shippingChargedToClient, DeliveryAgency agency)
    {
        if (!agency.CanCollectCashOnDelivery)
        {
            if (paymentTotal < saleTotal)
            {
                throw new AppBadRequestException("La venta debe estar pagada completamente antes de enviarla con una agencia que no recauda efectivo.");
            }

            return 0;
        }

        return Math.Round(Math.Max(0, saleTotal - paymentTotal) + shippingChargedToClient, 2);
    }

    private static void Normalize(CreateSaleDeliveryDTO delivery)
    {
        delivery.Code = delivery.Code.NormalizeRequired("Codigo del envio");
        delivery.DeliveryAddress = delivery.DeliveryAddress.NormalizeOptional();
        delivery.Comments = delivery.Comments.NormalizeOptional();
    }

    private static void Normalize(PatchSaleDeliveryDTO delivery)
    {
        if (delivery.HasCode) delivery.Code = delivery.Code.NormalizeRequired("Codigo del envio");
        if (delivery.HasDeliveryAddress) delivery.DeliveryAddress = delivery.DeliveryAddress.NormalizeOptional();
    }

    private static void ValidateAmounts(CreateSaleDeliveryDTO delivery)
    {
        if (delivery.ShippingChargedToClient < 0)
        {
            throw new AppBadRequestException("El monto de envio cobrado a la clienta no puede ser negativo.");
        }
    }

    private static void ValidateAmounts(PatchSaleDeliveryDTO delivery)
    {
        if (delivery.HasDeliveryAgencyId && !delivery.DeliveryAgencyId.HasValue)
            throw new AppBadRequestException("La agencia de envio no puede ser nula.");
        if (delivery.HasMunicipalityId && !delivery.MunicipalityId.HasValue)
            throw new AppBadRequestException("El municipio no puede ser nulo.");
        if (delivery.HasShippingChargedToClient && (!delivery.ShippingChargedToClient.HasValue || delivery.ShippingChargedToClient.Value < 0))
            throw new AppBadRequestException("El monto de envio cobrado a la clienta no puede ser negativo.");
    }
    private string ResolveUserId() => _currentUserService.UserId ?? "system";
}
