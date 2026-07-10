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
        var payments = await CreateSalePaymentMovementsAsync(createSaleDTO.PaymentMovements);
        var totals = CalculateSaleTotals(saleProducts);
        var paymentTotal = CalculatePaymentTotal(payments);
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
            PaymentMovements = payments
        };

        if (SaleAffectsInventory(createSaleDTO.SaleStatusId))
        {
            ApplyInventoryOutput(sale);
        }

        foreach (var payment in sale.PaymentMovements)
        {
            await _context.FinancialMovements.AddAsync(CreateFinancialMovementForSalePaymentMovement(payment, exchangeRate));
        }

        await _context.Sales.AddAsync(sale);
        await _context.SaveChangesAsync();

        return sale.Id;
    }

    public async Task PatchHeaderAsync(int id, PatchSaleHeaderDTO patchSaleHeaderDTO)
    {
        NormalizeSaleFields(patchSaleHeaderDTO);
        await ValidateSaleHeaderPatchAsync(patchSaleHeaderDTO);

        var sale = await GetSaleWithDetailsAsync(id, asNoTracking: false);
        EnsureSaleHeaderCanBePatched(sale);

        var saleChannelId = patchSaleHeaderDTO.HasSaleChannelId
            ? patchSaleHeaderDTO.SaleChannelId!.Value
            : sale.SaleChannelId;
        var paymentTotal = CalculatePaymentTotal(sale.PaymentMovements);
        EnsurePaymentRules(saleChannelId, sale.Total, paymentTotal, allowOverpayment: true);

        if (patchSaleHeaderDTO.HasSaleDate)
        {
            sale.SaleDate = patchSaleHeaderDTO.SaleDate!.Value;
            SyncInventoryMovementDates(sale);
        }

        sale.SaleChannelId = saleChannelId;

        if (patchSaleHeaderDTO.HasClientId)
        {
            sale.ClientId = patchSaleHeaderDTO.ClientId;
        }

        if (patchSaleHeaderDTO.HasMunicipalityId)
        {
            sale.MunicipalityId = patchSaleHeaderDTO.MunicipalityId;
        }

        if (patchSaleHeaderDTO.HasComments)
        {
            sale.Comments = patchSaleHeaderDTO.Comments;
        }

        await _context.SaveChangesAsync();
    }

    public async Task ReplaceProductsAsync(int id, ReplaceSaleProductsDTO replaceSaleProductsDTO)
    {
        NormalizeSaleFields(replaceSaleProductsDTO);
        await ValidateSaleProductsReplacementAsync(replaceSaleProductsDTO);

        var sale = await GetSaleWithDetailsAsync(id, asNoTracking: false);
        EnsureSaleProductsCanBeReplaced(sale);

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
        var totals = CalculateSaleTotals(saleProducts);

        var paymentTotal = CalculatePaymentTotal(sale.PaymentMovements);
        EnsurePaymentRules(sale.SaleChannelId, totals.Total, paymentTotal, allowOverpayment: true);

        sale.SalePaymentStatusId = ResolvePaymentStatus(totals.Total, paymentTotal);
        sale.Subtotal = totals.Subtotal;
        sale.TotalDiscount = totals.TotalDiscount;
        sale.Total = totals.Total;
        sale.Products = saleProducts;

        if (SaleAffectsInventory(sale.SaleStatusId))
        {
            ApplyInventoryOutput(sale);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> AddPaymentMovementAsync(int saleId, CreateSalePaymentMovementDTO createPaymentMovementDTO)
    {
        await ValidatePaymentRequestAsync(createPaymentMovementDTO);

        var sale = await GetSaleWithDetailsAsync(saleId, asNoTracking: false);
        EnsureSalePaymentMovementsCanBeChanged(sale);

        var payment = await CreateSalePaymentMovementAsync(createPaymentMovementDTO);
        var paymentTotal = CalculatePaymentTotal(sale.PaymentMovements) + payment.GrossAmount;
        EnsurePaymentRules(sale.SaleChannelId, sale.Total, paymentTotal);

        sale.PaymentMovements.Add(payment);
        sale.SalePaymentStatusId = ResolvePaymentStatus(sale.Total, paymentTotal);

        var exchangeRate = await GetCurrentExchangeRateAsync();
        await _context.FinancialMovements.AddAsync(CreateFinancialMovementForSalePaymentMovement(payment, exchangeRate));
        await _context.SaveChangesAsync();

        return payment.Id;
    }

    public async Task UpdatePaymentMovementAsync(int saleId, int paymentMovementId, UpdateSalePaymentMovementDTO updatePaymentMovementDTO)
    {
        await ValidatePaymentRequestAsync(updatePaymentMovementDTO);

        var sale = await GetSaleWithDetailsAsync(saleId, asNoTracking: false);
        EnsureSalePaymentMovementsCanBeChanged(sale);

        var payment = GetSalePaymentMovement(sale, paymentMovementId);
        EnsureSalePaymentMovementCanBeUpdated(payment);

        var currentPaymentTotal = CalculatePaymentTotal(sale.PaymentMovements);
        var updatedPaymentTotal = currentPaymentTotal - payment.GrossAmount + updatePaymentMovementDTO.GrossAmount;
        EnsurePaymentRules(sale.SaleChannelId, sale.Total, updatedPaymentTotal);

        await ApplyPaymentAmountsAsync(
            payment,
            updatePaymentMovementDTO.MovementDate,
            updatePaymentMovementDTO.PaymentMethodId,
            updatePaymentMovementDTO.PaymentTerminalId,
            updatePaymentMovementDTO.GrossAmount);

        sale.SalePaymentStatusId = ResolvePaymentStatus(sale.Total, updatedPaymentTotal);
        await SyncFinancialMovementAsync(payment);
        await _context.SaveChangesAsync();
    }

    public async Task<int> RefundPaymentMovementAsync(int saleId, int paymentMovementId, RefundSalePaymentMovementDTO refundPaymentMovementDTO)
    {
        var sale = await GetSaleWithDetailsAsync(saleId, asNoTracking: false);
        EnsureSalePaymentMovementsCanBeChanged(sale);

        var originalPayment = GetSalePaymentMovement(sale, paymentMovementId);
        EnsureSalePaymentMovementCanBeRefunded(originalPayment);

        var refund = await CreateRefundPaymentMovementAsync(originalPayment, refundPaymentMovementDTO);
        var paymentTotal = CalculatePaymentTotal(sale.PaymentMovements) - refund.GrossAmount;
        EnsureRefundDoesNotBreakInStorePaymentRule(sale, paymentTotal);

        sale.PaymentMovements.Add(refund);
        sale.SalePaymentStatusId = ResolvePaymentStatus(sale.Total, paymentTotal);

        var exchangeRate = await GetCurrentExchangeRateAsync();
        await _context.FinancialMovements.AddAsync(CreateFinancialMovementForSalePaymentMovement(refund, exchangeRate));
        await _context.SaveChangesAsync();

        return refund.Id;
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
            .Include(sale => sale.PaymentMovements)
                .ThenInclude(payment => payment.PaymentMethod)
            .Include(sale => sale.PaymentMovements)
                .ThenInclude(payment => payment.PaymentTerminal)
            .Include(sale => sale.PaymentMovements)
                .ThenInclude(payment => payment.ReversalMovements)
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
        await ValidatePaymentRequestAsync(saleDTO.PaymentMovements);
    }

    private async Task ValidateSaleHeaderPatchAsync(PatchSaleHeaderDTO saleDTO)
    {
        if (saleDTO.HasSaleDate && !saleDTO.SaleDate.HasValue)
        {
            throw new AppBadRequestException("La fecha de la venta no puede ser nula.");
        }

        if (saleDTO.HasSaleChannelId && !saleDTO.SaleChannelId.HasValue)
        {
            throw new AppBadRequestException("El canal de venta no puede ser nulo.");
        }

        if (saleDTO.SaleChannelId.HasValue &&
            !await _context.SaleChannels.AnyAsync(channel => channel.Id == saleDTO.SaleChannelId.Value))
        {
            throw new AppNotFoundException($"El canal de venta con id '{saleDTO.SaleChannelId.Value}' no existe.");
        }

        await ValidateOptionalReferencesAsync(saleDTO.ClientId, saleDTO.MunicipalityId);
    }

    private async Task ValidateSaleProductsReplacementAsync(ReplaceSaleProductsDTO saleDTO)
    {
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

    private async Task ValidatePaymentRequestAsync(List<CreateSalePaymentMovementDTO> payments)
    {
        foreach (var payment in payments)
        {
            await ValidatePaymentRequestAsync(payment);
        }
    }

    private async Task ValidatePaymentRequestAsync(CreateSalePaymentMovementDTO payment)
    {
        await ValidatePaymentDataAsync(payment.PaymentMethodId, payment.PaymentTerminalId, payment.GrossAmount);
    }

    private async Task ValidatePaymentRequestAsync(UpdateSalePaymentMovementDTO payment)
    {
        await ValidatePaymentDataAsync(payment.PaymentMethodId, payment.PaymentTerminalId, payment.GrossAmount);
    }

    private async Task ValidatePaymentDataAsync(int paymentMethodId, int? paymentTerminalId, decimal grossAmount)
    {
        if (grossAmount <= 0)
        {
            throw new AppBadRequestException("El monto de cada pago debe ser mayor que cero.");
        }

        if (!await _context.PaymentMethods.AnyAsync(method => method.Id == paymentMethodId))
        {
            throw new AppNotFoundException($"El metodo de pago con id '{paymentMethodId}' no existe.");
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

    private async Task<List<SalePaymentMovement>> CreateSalePaymentMovementsAsync(List<CreateSalePaymentMovementDTO> paymentRequests)
    {
        var payments = new List<SalePaymentMovement>();
        foreach (var paymentRequest in paymentRequests)
        {
            payments.Add(await CreateSalePaymentMovementAsync(paymentRequest));
        }

        return payments;
    }

    private async Task<SalePaymentMovement> CreateSalePaymentMovementAsync(CreateSalePaymentMovementDTO paymentRequest)
    {
        var payment = new SalePaymentMovement
        {
            MovementDirectionId = (int)MovementDirectionOptions.In,
            UserId = ResolveUserId()
        };

        await ApplyPaymentAmountsAsync(
            payment,
            paymentRequest.MovementDate,
            paymentRequest.PaymentMethodId,
            paymentRequest.PaymentTerminalId,
            paymentRequest.GrossAmount);

        return payment;
    }

    private async Task ApplyPaymentAmountsAsync(
        SalePaymentMovement payment,
        DateTime? movementDate,
        int paymentMethodId,
        int? paymentTerminalId,
        decimal grossAmount)
    {
        var terminal = paymentTerminalId.HasValue
            ? await _context.PaymentTerminals.FirstAsync(item => item.Id == paymentTerminalId.Value)
            : null;

        var commissionAmount = terminal is null
            ? 0
            : Math.Round(grossAmount * terminal.ComissionPercentage / 100, 2);
        var incomeTaxAmount = terminal is null
            ? 0
            : Math.Round((grossAmount - commissionAmount) * terminal.IncomeTaxPercentage / 100, 2);

        payment.MovementDate = movementDate ?? DateTime.UtcNow;
        payment.PaymentMethodId = paymentMethodId;
        payment.PaymentTerminalId = paymentTerminalId;
        payment.GrossAmount = grossAmount;
        payment.CommissionPercentage = terminal?.ComissionPercentage ?? 0;
        payment.CommissionAmount = commissionAmount;
        payment.IncomeTaxPercentage = terminal?.IncomeTaxPercentage ?? 0;
        payment.IncomeTaxAmount = incomeTaxAmount;
        payment.NetReceivedAmount = grossAmount - commissionAmount - incomeTaxAmount;
    }

    private async Task<SalePaymentMovement> CreateRefundPaymentMovementAsync(
        SalePaymentMovement originalPayment,
        RefundSalePaymentMovementDTO refundPaymentMovementDTO)
    {
        var refundedAmount = originalPayment.ReversalMovements.Sum(movement => movement.GrossAmount);
        var remainingRefundableAmount = originalPayment.GrossAmount - refundedAmount;

        if (remainingRefundableAmount <= 0)
        {
            throw new AppBadRequestException("El pago ya fue reembolsado completamente.");
        }

        if (originalPayment.PaymentMethodId == (int)PaymentMethodOption.Card)
        {
            return CreateCardRefundPaymentMovement(originalPayment, refundPaymentMovementDTO, refundedAmount);
        }

        var refundPaymentMethodId = refundPaymentMovementDTO.PaymentMethodId ?? originalPayment.PaymentMethodId;
        if (refundPaymentMethodId == (int)PaymentMethodOption.Card)
        {
            throw new AppBadRequestException("Solo se puede reembolsar por tarjeta cuando el pago original fue por tarjeta.");
        }

        if (refundPaymentMovementDTO.PaymentTerminalId.HasValue)
        {
            throw new AppBadRequestException("Los reembolsos en efectivo o transferencia no deben asociarse a una terminal de pago.");
        }

        var refundAmount = refundPaymentMovementDTO.GrossAmount ?? remainingRefundableAmount;
        if (refundAmount <= 0 || refundAmount > remainingRefundableAmount)
        {
            throw new AppBadRequestException("El monto del reembolso no puede exceder el saldo pendiente de reembolsar del pago.");
        }

        await ValidatePaymentDataAsync(refundPaymentMethodId, null, refundAmount);

        return new SalePaymentMovement
        {
            MovementDate = refundPaymentMovementDTO.MovementDate ?? DateTime.UtcNow,
            MovementDirectionId = (int)MovementDirectionOptions.Out,
            PaymentMethodId = refundPaymentMethodId,
            ReversedSalePaymentMovementId = originalPayment.Id,
            GrossAmount = refundAmount,
            CommissionPercentage = 0,
            CommissionAmount = 0,
            IncomeTaxPercentage = 0,
            IncomeTaxAmount = 0,
            NetReceivedAmount = refundAmount,
            UserId = ResolveUserId()
        };
    }

    private SalePaymentMovement CreateCardRefundPaymentMovement(
        SalePaymentMovement originalPayment,
        RefundSalePaymentMovementDTO refundPaymentMovementDTO,
        decimal refundedAmount)
    {
        if (refundedAmount > 0)
        {
            throw new AppBadRequestException("Los pagos por tarjeta solo se pueden anular una vez y por el monto completo.");
        }

        if (refundPaymentMovementDTO.PaymentMethodId.HasValue && refundPaymentMovementDTO.PaymentMethodId.Value != originalPayment.PaymentMethodId)
        {
            throw new AppBadRequestException("El reembolso de una tarjeta debe usar el mismo metodo de pago del pago original.");
        }

        if (refundPaymentMovementDTO.PaymentTerminalId.HasValue && refundPaymentMovementDTO.PaymentTerminalId.Value != originalPayment.PaymentTerminalId)
        {
            throw new AppBadRequestException("El reembolso de una tarjeta debe usar la misma terminal del pago original.");
        }

        if (refundPaymentMovementDTO.GrossAmount.HasValue && refundPaymentMovementDTO.GrossAmount.Value != originalPayment.GrossAmount)
        {
            throw new AppBadRequestException("Los pagos por tarjeta solo se pueden anular por el monto completo.");
        }

        return new SalePaymentMovement
        {
            MovementDate = refundPaymentMovementDTO.MovementDate ?? DateTime.UtcNow,
            MovementDirectionId = (int)MovementDirectionOptions.Out,
            PaymentMethodId = originalPayment.PaymentMethodId,
            PaymentTerminalId = originalPayment.PaymentTerminalId,
            ReversedSalePaymentMovementId = originalPayment.Id,
            GrossAmount = originalPayment.GrossAmount,
            CommissionPercentage = originalPayment.CommissionPercentage,
            CommissionAmount = originalPayment.CommissionAmount,
            IncomeTaxPercentage = originalPayment.IncomeTaxPercentage,
            IncomeTaxAmount = originalPayment.IncomeTaxAmount,
            NetReceivedAmount = originalPayment.NetReceivedAmount,
            UserId = ResolveUserId()
        };
    }

    private static SaleTotals CalculateSaleTotals(List<SaleProduct> products)
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

        if (paymentTotal > amountToCharge)
        {
            return (int)SalePaymentStatusOption.RefundPending;
        }

        return (int)SalePaymentStatusOption.Paid;
    }

    private static void EnsurePaymentRules(int saleChannelId, decimal amountToCharge, decimal paymentTotal, bool allowOverpayment = false)
    {
        if (!allowOverpayment && paymentTotal > amountToCharge)
        {
            throw new AppBadRequestException("La suma de pagos no puede exceder el total de la venta.");
        }

        if (saleChannelId == (int)SaleChannelOption.InStoreSale && !allowOverpayment && paymentTotal != amountToCharge)
        {
            throw new AppBadRequestException("Las ventas en local deben quedar pagadas completamente al momento de registrarse.");
        }

        if (saleChannelId == (int)SaleChannelOption.InStoreSale && allowOverpayment && paymentTotal < amountToCharge)
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

    private static void SyncInventoryMovementDates(Sale sale)
    {
        if (!SaleAffectsInventory(sale.SaleStatusId))
        {
            return;
        }

        foreach (var saleProduct in sale.Products)
        {
            var movement = saleProduct.Product?.InventoryMovements
                .FirstOrDefault(item => item.SaleProductId == saleProduct.Id);

            if (movement is not null)
            {
                movement.MovementDate = sale.SaleDate;
            }
        }
    }

    private static void EnsureSaleHeaderCanBePatched(Sale sale)
    {
        if (sale.SaleStatusId == (int)SaleStatusOption.Cancelled)
        {
            throw new AppBadRequestException("No se puede actualizar la cabecera de una venta cancelada.");
        }
    }

    private static void EnsureSaleProductsCanBeReplaced(Sale sale)
    {
        if (sale.SaleStatusId is (int)SaleStatusOption.Cancelled or (int)SaleStatusOption.SentForDelivery or (int)SaleStatusOption.Completed)
        {
            throw new AppBadRequestException("No se pueden reemplazar los productos de una venta cancelada, enviada o completada.");
        }
    }

    private static void EnsureSalePaymentMovementsCanBeChanged(Sale sale)
    {
        if (sale.SaleStatusId == (int)SaleStatusOption.Cancelled)
        {
            throw new AppBadRequestException("No se pueden modificar los pagos de una venta cancelada.");
        }
    }

    private static SalePaymentMovement GetSalePaymentMovement(Sale sale, int paymentMovementId)
    {
        return sale.PaymentMovements.FirstOrDefault(payment => payment.Id == paymentMovementId)
            ?? throw new AppNotFoundException($"El movimiento de pago con id '{paymentMovementId}' no existe para la venta indicada.");
    }

    private static void EnsureSalePaymentMovementCanBeUpdated(SalePaymentMovement payment)
    {
        if (payment.MovementDirectionId != (int)MovementDirectionOptions.In)
        {
            throw new AppBadRequestException("Solo se pueden actualizar pagos de entrada. Los reembolsos deben registrarse como movimientos nuevos.");
        }

        if (payment.ReversalMovements.Count > 0)
        {
            throw new AppBadRequestException("No se puede actualizar un pago que ya tiene reembolsos registrados.");
        }
    }

    private static void EnsureSalePaymentMovementCanBeRefunded(SalePaymentMovement payment)
    {
        if (payment.MovementDirectionId != (int)MovementDirectionOptions.In)
        {
            throw new AppBadRequestException("Solo se pueden reembolsar pagos de entrada.");
        }
    }

    private static void EnsureRefundDoesNotBreakInStorePaymentRule(Sale sale, decimal paymentTotal)
    {
        if (sale.SaleChannelId == (int)SaleChannelOption.InStoreSale && paymentTotal < sale.Total)
        {
            throw new AppBadRequestException("Las ventas en local no pueden quedar con pago menor al total despues del reembolso.");
        }
    }

    private async Task SyncFinancialMovementAsync(SalePaymentMovement payment)
    {
        var movement = await _context.FinancialMovements
            .FirstOrDefaultAsync(item => item.SalePaymentMovementId == payment.Id);

        if (movement is null)
        {
            var exchangeRate = await GetCurrentExchangeRateAsync();
            await _context.FinancialMovements.AddAsync(CreateFinancialMovementForSalePaymentMovement(payment, exchangeRate));
            return;
        }

        movement.Description = payment.MovementDirectionId == (int)MovementDirectionOptions.In
            ? "Pago de venta."
            : "Reembolso de venta.";
        movement.MovementDate = payment.MovementDate;
        movement.MovementDirectionId = payment.MovementDirectionId;
        movement.FinancialMovementTypeId = payment.MovementDirectionId == (int)MovementDirectionOptions.In
            ? (int)FinancialMovementTypeOption.SalePayment
            : (int)FinancialMovementTypeOption.CustomerRefund;
        movement.Amount = payment.NetReceivedAmount;
    }

    private static decimal CalculatePaymentTotal(IEnumerable<SalePaymentMovement> payments)
    {
        return payments.Sum(payment => payment.MovementDirectionId == (int)MovementDirectionOptions.Out
            ? -payment.GrossAmount
            : payment.GrossAmount);
    }

    private static void NormalizeSaleFields(CreateSaleDTO saleDTO)
    {
        saleDTO.Comments = saleDTO.Comments.NormalizeOptional();
        saleDTO.Products ??= [];
        saleDTO.PaymentMovements ??= [];
    }

    private static void NormalizeSaleFields(PatchSaleHeaderDTO saleDTO)
    {
        if (saleDTO.HasComments)
        {
            saleDTO.Comments = saleDTO.Comments.NormalizeOptional();
        }
    }

    private static void NormalizeSaleFields(ReplaceSaleProductsDTO saleDTO)
    {
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

    private static FinancialMovement CreateFinancialMovementForSalePaymentMovement(SalePaymentMovement payment, decimal exchangeRate)
    {
        return new FinancialMovement
        {
            Description = payment.MovementDirectionId == (int)MovementDirectionOptions.In ? "Pago de venta." : "Reembolso de venta.",
            MovementDate = payment.MovementDate,
            MovementDirectionId = payment.MovementDirectionId,
            FinancialMovementTypeId = payment.MovementDirectionId == (int)MovementDirectionOptions.In ? (int)FinancialMovementTypeOption.SalePayment : (int)FinancialMovementTypeOption.CustomerRefund,
            SalePaymentMovement = payment,
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
