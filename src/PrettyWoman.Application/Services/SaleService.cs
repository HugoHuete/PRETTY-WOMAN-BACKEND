using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Sales;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class SaleService(IApplicationDbContext context, ICurrentUserService currentUserService) : ISaleService
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<IEnumerable<SaleDTO>> GetAllAsync()
    {
        var sales = await _context.Sales
            .AsNoTracking()
            .Include(sale => sale.SaleChannel)
            .Include(sale => sale.SaleStatus)
            .Include(sale => sale.SalePaymentStatus)
            .Include(sale => sale.Client)
            .Include(sale => sale.Municipality)
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

        var products = await LoadSaleProductsAsync(createSaleDTO.Products);
        var saleProducts = CreateSaleProducts(createSaleDTO.Products, products);
        var payments = await CreateSalePaymentsAsync(createSaleDTO.Payments);
        var totals = CalculateSaleTotals(saleProducts, payments);
        var paymentTotal = payments.Sum(payment => payment.GrossAmount);
        var exchangeRate = payments.Count == 0 ? 0 : await GetCurrentExchangeRateAsync();

        EnsurePaymentRules(createSaleDTO.SaleChannelId, totals.Total, paymentTotal);

        var sale = new Sale
        {
            SaleDate = createSaleDTO.SaleDate ?? DateTime.UtcNow,
            SaleChannelId = createSaleDTO.SaleChannelId,
            SaleStatusId = createSaleDTO.SaleStatusId,
            SalePaymentStatusId = ResolvePaymentStatus(totals.Total, paymentTotal),
            UserId = ResolveUserId(),
            Subtotal = totals.Subtotal,
            TotalDiscount = totals.TotalDiscount,
            Total = totals.Total,
            Comments = createSaleDTO.Comments,
            ClientId = createSaleDTO.ClientId,
            MunicipalityId = createSaleDTO.MunicipalityId,
            Products = saleProducts,
            Payments = payments
        };

        if (SaleAffectsInventory(createSaleDTO.SaleStatusId))
        {
            ApplyInventoryOutput(sale);
        }

        foreach (var payment in sale.Payments)
        {
            await _context.FinancialMovements.AddAsync(CreateSalePaymentMovement(payment, exchangeRate));
        }

        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

        return sale.Id;
    }

    public async Task UpdateAsync(int id, UpdateSaleDTO updateSaleDTO)
    {
        NormalizeSaleFields(updateSaleDTO);
        await ValidateSaleRequestAsync(updateSaleDTO);

        var sale = await GetSaleWithDetailsAsync(id, asNoTracking: false);
        EnsureSaleCanBeUpdated(sale);

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

        var products = await LoadSaleProductsAsync(updateSaleDTO.Products);
        var saleProducts = CreateSaleProducts(updateSaleDTO.Products, products);
        var totals = CalculateSaleTotals(saleProducts, []);

        EnsurePaymentRules(updateSaleDTO.SaleChannelId, totals.Total, paymentTotal: 0);

        sale.SaleDate = updateSaleDTO.SaleDate ?? sale.SaleDate;
        sale.SaleChannelId = updateSaleDTO.SaleChannelId;
        sale.SaleStatusId = updateSaleDTO.SaleStatusId;
        sale.SalePaymentStatusId = (int)SalePaymentStatusOption.Unpaid;
        sale.Subtotal = totals.Subtotal;
        sale.TotalDiscount = totals.TotalDiscount;
        sale.Total = totals.Total;
        sale.Comments = updateSaleDTO.Comments;
        sale.ClientId = updateSaleDTO.ClientId;
        sale.MunicipalityId = updateSaleDTO.MunicipalityId;
        sale.Products = saleProducts;

        if (SaleAffectsInventory(updateSaleDTO.SaleStatusId))
        {
            ApplyInventoryOutput(sale);
        }

        await _context.SaveChangesAsync();
    }

    private async Task<Sale> GetSaleWithDetailsAsync(int id, bool asNoTracking)
    {
        var query = _context.Sales
            .Include(sale => sale.SaleChannel)
            .Include(sale => sale.SaleStatus)
            .Include(sale => sale.SalePaymentStatus)
            .Include(sale => sale.Client)
            .Include(sale => sale.Municipality)
            .Include(sale => sale.Products)
                .ThenInclude(saleProduct => saleProduct.Product!)
                    .ThenInclude(product => product.InventoryMovements)
            .Include(sale => sale.Products)
                .ThenInclude(saleProduct => saleProduct.DiscountSource)
            .Include(sale => sale.Products)
                .ThenInclude(saleProduct => saleProduct.SaleProductStatus)
            .Include(sale => sale.Payments)
                .ThenInclude(payment => payment.PaymentMethod)
            .Include(sale => sale.Payments)
                .ThenInclude(payment => payment.PaymentTerminal)
            .Include(sale => sale.Deliveries)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(sale => sale.Id == id)
            ?? throw new AppNotFoundException($"La venta con id '{id}' no existe.");
    }

    private async Task ValidateSaleRequestAsync(CreateSaleDTO saleDTO)
    {
        if (!await _context.SaleChannels.AnyAsync(channel => channel.Id == saleDTO.SaleChannelId))
        {
            throw new AppNotFoundException($"El canal de venta con id '{saleDTO.SaleChannelId}' no existe.");
        }

        await ValidateOptionalReferencesAsync(saleDTO.ClientId, saleDTO.MunicipalityId);
        await ValidateRequestedSaleStatusAsync(saleDTO.SaleStatusId);
        await ValidateProductRequestAsync(saleDTO.Products);
        await ValidatePaymentRequestAsync(saleDTO.Payments);
    }

    private async Task ValidateSaleRequestAsync(UpdateSaleDTO saleDTO)
    {
        if (!await _context.SaleChannels.AnyAsync(channel => channel.Id == saleDTO.SaleChannelId))
        {
            throw new AppNotFoundException($"El canal de venta con id '{saleDTO.SaleChannelId}' no existe.");
        }

        await ValidateOptionalReferencesAsync(saleDTO.ClientId, saleDTO.MunicipalityId);
        await ValidateRequestedSaleStatusAsync(saleDTO.SaleStatusId);
        await ValidateProductRequestAsync(saleDTO.Products);
    }

    private async Task ValidateOptionalReferencesAsync(int? clientId, int? municipalityId)
    {
        if (clientId.HasValue && !await _context.Clients.AnyAsync(client => client.Id == clientId.Value))
        {
            throw new AppNotFoundException($"La clienta con id '{clientId.Value}' no existe.");
        }

        if (municipalityId.HasValue && !await _context.Municipalities.AnyAsync(municipality => municipality.Id == municipalityId.Value))
        {
            throw new AppNotFoundException($"El municipio con id '{municipalityId.Value}' no existe.");
        }
    }

    private async Task ValidateRequestedSaleStatusAsync(int? saleStatusId)
    {
        if (!saleStatusId.HasValue)
        {
            return;
        }

        var status = (SaleStatusOption)saleStatusId.Value;
        var allowedInitialStatus = status is SaleStatusOption.Pending
            or SaleStatusOption.Reserved
            or SaleStatusOption.ReadyForDelivery;

        if (!allowedInitialStatus || !await _context.SaleStatuses.AnyAsync(saleStatus => saleStatus.Id == saleStatusId.Value))
        {
            throw new AppBadRequestException("El estado inicial de la venta no es valido.");
        }
    }

    private async Task ValidateProductRequestAsync(List<CreateSaleProductDTO> products)
    {
        if (products.Count == 0)
        {
            throw new AppBadRequestException("Debe enviar al menos un producto para la venta.");
        }

        foreach (var product in products)
        {
            if (product.Quantity <= 0)
            {
                throw new AppBadRequestException("La cantidad de cada producto debe ser mayor que cero.");
            }

            if (product.DiscountAmount < 0)
            {
                throw new AppBadRequestException("El descuento no puede ser negativo.");
            }

            if (!await _context.DiscountSources.AnyAsync(source => source.Id == product.DiscountSourceId))
            {
                throw new AppNotFoundException($"La fuente de descuento con id '{product.DiscountSourceId}' no existe.");
            }

            if (product.DiscountCampaignId.HasValue &&
                !await _context.DiscountCampaigns.AnyAsync(campaign => campaign.Id == product.DiscountCampaignId.Value))
            {
                throw new AppNotFoundException($"La campana de descuento con id '{product.DiscountCampaignId.Value}' no existe.");
            }
        }
    }

    private async Task ValidatePaymentRequestAsync(List<CreateSalePaymentDTO> payments)
    {
        foreach (var payment in payments)
        {
            if (payment.GrossAmount <= 0)
            {
                throw new AppBadRequestException("El monto de cada pago debe ser mayor que cero.");
            }

            if (!await _context.PaymentMethods.AnyAsync(method => method.Id == payment.PaymentMethodId))
            {
                throw new AppNotFoundException($"El metodo de pago con id '{payment.PaymentMethodId}' no existe.");
            }

            if (payment.PaymentTerminalId.HasValue &&
                !await _context.PaymentTerminals.AnyAsync(terminal => terminal.Id == payment.PaymentTerminalId.Value && terminal.Enabled))
            {
                throw new AppBadRequestException($"La terminal de pago con id '{payment.PaymentTerminalId.Value}' no existe o no esta habilitada.");
            }
        }
    }

    private async Task<List<Product>> LoadSaleProductsAsync(List<CreateSaleProductDTO> productRequests)
    {
        var productIds = productRequests.Select(product => product.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();

        var missingProductId = productIds.FirstOrDefault(id => products.All(product => product.Id != id));
        if (missingProductId != 0)
        {
            throw new AppNotFoundException($"El producto con id '{missingProductId}' no existe.");
        }

        var requestedQuantities = productRequests
            .GroupBy(product => product.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(product => product.Quantity));

        foreach (var product in products)
        {
            if (product.AvailableQuantity < requestedQuantities[product.Id])
            {
                throw new AppBadRequestException($"El producto con id '{product.Id}' no tiene stock disponible suficiente.");
            }
        }

        return products;
    }

    private static List<SaleProduct> CreateSaleProducts(List<CreateSaleProductDTO> productRequests, List<Product> products)
    {
        return productRequests.Select(productRequest =>
        {
            var product = products.First(product => product.Id == productRequest.ProductId);
            if (productRequest.DiscountAmount > product.SalePrice)
            {
                throw new AppBadRequestException("El descuento no puede ser mayor que el precio de venta.");
            }

            var finalUnitPrice = Math.Round(product.SalePrice - productRequest.DiscountAmount, 2);
            var lineTotal = Math.Round(finalUnitPrice * productRequest.Quantity, 2);
            var totalCostAtSale = Math.Round(product.UnitCostNio * productRequest.Quantity, 6);

            return new SaleProduct
            {
                ProductId = product.Id,
                Product = product,
                Quantity = productRequest.Quantity,
                UnitCostAtSale = product.UnitCostNio,
                OriginalUnitPrice = product.SalePrice,
                DiscountSourceId = productRequest.DiscountSourceId,
                DiscountCampaignId = productRequest.DiscountCampaignId,
                DiscountAmount = productRequest.DiscountAmount,
                FinalUnitPrice = finalUnitPrice,
                LineTotal = lineTotal,
                TotalCostAtSale = totalCostAtSale,
                GrossProfit = lineTotal - totalCostAtSale,
                SaleProductStatusId = (int)SaleProductStatusOption.Completed,
                
            };
        }).ToList();
    }

    private async Task<List<SalePayment>> CreateSalePaymentsAsync(List<CreateSalePaymentDTO> paymentRequests)
    {
        var terminalIds = paymentRequests
            .Where(payment => payment.PaymentTerminalId.HasValue)
            .Select(payment => payment.PaymentTerminalId!.Value)
            .Distinct()
            .ToList();

        var terminals = terminalIds.Count == 0
            ? []
            : await _context.PaymentTerminals
                .Where(terminal => terminalIds.Contains(terminal.Id))
                .ToListAsync();

        return paymentRequests.Select(paymentRequest =>
        {
            var terminal = paymentRequest.PaymentTerminalId.HasValue
                ? terminals.First(item => item.Id == paymentRequest.PaymentTerminalId.Value)
                : null;

            var commissionAmount = terminal is null
                ? 0
                : Math.Round(paymentRequest.GrossAmount * terminal.ComissionPercentage / 100, 2);
            
            // The income tax is calculated on the amount after deducting the commission from the gross amount.
            var incomeTaxAmount = terminal is null
                ? 0
                : Math.Round((paymentRequest.GrossAmount - commissionAmount) * terminal.IncomeTaxPercentage / 100, 2);

            return new SalePayment
            {
                PaymentDate = paymentRequest.PaymentDate ?? DateTime.UtcNow,
                PaymentMethodId = paymentRequest.PaymentMethodId,
                PaymentTerminalId = paymentRequest.PaymentTerminalId,
                GrossAmount = paymentRequest.GrossAmount,
                CommissionPercentage = terminal?.ComissionPercentage ?? 0,
                CommissionAmount = commissionAmount,
                IncomeTaxPercentage = terminal?.IncomeTaxPercentage ?? 0,
                IncomeTaxAmount = incomeTaxAmount,
                NetReceivedAmount = paymentRequest.GrossAmount - commissionAmount - incomeTaxAmount,
                UserId = ResolveUserId()
            };
        }).ToList();
    }

    private static SaleTotals CalculateSaleTotals(List<SaleProduct> products, List<SalePayment> payments)
    {
        var subtotal = Math.Round(products.Sum(product => product.OriginalUnitPrice * product.Quantity), 2);
        var totalDiscount = Math.Round(products.Sum(product => product.DiscountAmount * product.Quantity), 2);
        var total = Math.Round(products.Sum(product => product.LineTotal), 2);

        return new SaleTotals(
            subtotal,
            totalDiscount,
            total);
    }

    private static int ResolvePaymentStatus(decimal amountToCharge, decimal paymentTotal)
    {
        if (paymentTotal == 0)
        {
            return (int)SalePaymentStatusOption.Unpaid;
        }

        if (paymentTotal < amountToCharge)
        {
            return (int)SalePaymentStatusOption.PartiallyPaid;
        }

        return (int)SalePaymentStatusOption.Paid;
    }

    private static void EnsurePaymentRules(int saleChannelId, decimal amountToCharge, decimal paymentTotal)
    {
        if (paymentTotal > amountToCharge)
        {
            throw new AppBadRequestException("La suma de pagos no puede exceder el total de la venta.");
        }

        if (saleChannelId == (int)SaleChannelOption.InStoreSale && paymentTotal != amountToCharge)
        {
            throw new AppBadRequestException("Las ventas en local deben quedar pagadas completamente al momento de registrarse.");
        }
    }

    private static bool SaleAffectsInventory(int saleStatusId)
    {
        var status = (SaleStatusOption)saleStatusId;
        return status is SaleStatusOption.Reserved
            or SaleStatusOption.ReadyForDelivery
            or SaleStatusOption.SentForDelivery
            or SaleStatusOption.Completed;
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
                Comments = $"Venta registrada."
            });
        }
    }

    private static void RevertInventoryOutput(Sale sale)
    {
        foreach (var saleProduct in sale.Products)
        {
            saleProduct.Product!.AvailableQuantity += saleProduct.Quantity;
        }
    }

    private static void EnsureSaleCanBeUpdated(Sale sale)
    {
        if (sale.SaleStatusId is (int)SaleStatusOption.Cancelled or (int)SaleStatusOption.SentForDelivery or (int)SaleStatusOption.Completed)
        {
            throw new AppBadRequestException("No se puede actualizar una venta cancelada, enviada o completada desde la actualizacion general.");
        }

        if (sale.Payments.Count > 0)
        {
            throw new AppBadRequestException("No se puede actualizar una venta con pagos registrados desde la actualizacion general.");
        }

        if (sale.Deliveries.Count > 0)
        {
            throw new AppBadRequestException("No se puede actualizar una venta con envios registrados desde la actualizacion general.");
        }
    }

    private static void NormalizeSaleFields(CreateSaleDTO saleDTO)
    {
        saleDTO.Comments = saleDTO.Comments.NormalizeOptional();
        saleDTO.Products ??= [];
        saleDTO.Payments ??= [];
    }

    private static void NormalizeSaleFields(UpdateSaleDTO saleDTO)
    {
        saleDTO.Comments = saleDTO.Comments.NormalizeOptional();
        saleDTO.Products ??= [];
    }

    private string ResolveUserId()
    {
        return _currentUserService.UserId ?? "system";
    }

    private async Task<decimal> GetCurrentExchangeRateAsync()
    {
        var exchangeRate = await _context.DollarExchangeRates
            .Where(rate => rate.Enabled)
            .OrderByDescending(rate => rate.StartDate)
            .Select(rate => (decimal?)rate.BankRate)
            .FirstOrDefaultAsync();

        return exchangeRate
            ?? throw new AppBadRequestException("Debe existir una tasa de cambio bancaria habilitada para registrar movimientos financieros.");
    }

    private static FinancialMovement CreateSalePaymentMovement(SalePayment payment, decimal exchangeRate)
    {
        return new FinancialMovement
        {
            Description = "Pago de venta.",
            MovementDate = payment.PaymentDate,
            MovementDirectionId = (int)MovementDirectionOptions.In,
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.SalePayment,
            SalePayment = payment,
            Amount = payment.NetReceivedAmount,
            ExchangeRate = exchangeRate,
            Comments = null
        };
    }

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
            MunicipalityId = sale.MunicipalityId,
            MunicipalityName = sale.Municipality?.Name,
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
            Payments = sale.Payments.Select(payment => new SalePaymentDTO
            {
                Id = payment.Id,
                PaymentDate = payment.PaymentDate,
                PaymentMethodId = payment.PaymentMethodId,
                PaymentMethodName = payment.PaymentMethod?.Name,
                PaymentTerminalId = payment.PaymentTerminalId,
                PaymentTerminalName = payment.PaymentTerminal?.Name,
                GrossAmount = payment.GrossAmount,
                CommissionPercentage = payment.CommissionPercentage,
                CommissionAmount = payment.CommissionAmount,
                IncomeTaxPercentage = payment.IncomeTaxPercentage,
                IncomeTaxAmount = payment.IncomeTaxAmount,
                NetReceivedAmount = payment.NetReceivedAmount
            }).ToList()
        };
    }

    private sealed record SaleTotals(
        decimal Subtotal,
        decimal TotalDiscount,
        decimal Total);
}





