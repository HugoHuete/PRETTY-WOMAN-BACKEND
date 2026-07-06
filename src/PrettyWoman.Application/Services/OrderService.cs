using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Orders;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class OrderService(IApplicationDbContext context, IMapper mapper) : IOrderService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<int> CreateAsync(CreateOrderDTO createOrderDTO)
    {
        NormalizeOrderFields(createOrderDTO);
        await ValidateOrderRequestAsync(createOrderDTO);

        var nextProductDetailCode = await GetNextProductDetailCodeAsync();
        var createdProducts = CreateProductDetails(createOrderDTO, nextProductDetailCode);
        var productDetails = createdProducts.ProductDetails;
        var products = productDetails.SelectMany(detail => detail.Products).ToList();
        var exchangeRate = await GetOrderExchangeRateAsync();
        var totals = CalculateCosts(createdProducts.ProductCosts, createOrderDTO.PurchaseCurrencyId, exchangeRate, createOrderDTO.SupplierShippingCostUsd);

        var order = new Order
        {
            PurchaseDate = createOrderDTO.PurchaseDate.NormalizeToUtc() ?? DateTime.UtcNow,
            OrderStatusId = (int)OrderStatusCode.Pending,
            SupplierId = createOrderDTO.SupplierId,
            PurchaseCurrencyId = createOrderDTO.PurchaseCurrencyId,
            AmountUsd = totals.AmountUsd,
            ExchangeRate = exchangeRate,
            MerchandiseTotalNio = totals.MerchandiseTotalNio,
            ReceivedAmountNio = 0,
            SupplierShippingCostUsd = createOrderDTO.SupplierShippingCostUsd,
            WarehouseShippingCostUsd = 0,
            TotalCostNio = totals.TotalCostNio,
            Comments = createOrderDTO.Comments,
            Products = products
        };

        await _context.Orders.AddAsync(order);
        if (order.TotalCostNio > 0)
        {
            await _context.FinancialMovements.AddAsync(CreateSupplierPaymentMovement(order));
        }

        await _context.SaveChangesAsync();

        return order.Id;
    }

    public async Task UpdateAsync(int id, UpdateOrderDTO updateOrderDTO)
    {
        NormalizeOrderFields(updateOrderDTO);
        await ValidateOrderRequestAsync(updateOrderDTO);

        var order = await _context.Orders
            .Include(order => order.Products)
                .ThenInclude(product => product.ProductDetail)
            .FirstOrDefaultAsync(order => order.Id == id)
            ?? throw new AppNotFoundException($"La orden con id '{id}' no existe.");

        await EnsureOrderProductsCanBeReplacedAsync(order);

        var oldProductDetailIds = order.Products
            .Select(product => product.ProductDetailId)
            .Distinct()
            .ToList();

        var oldProductDetails = await _context.ProductDetails
            .Where(productDetail => oldProductDetailIds.Contains(productDetail.Id))
            .OrderBy(productDetail => productDetail.Code)
            .ToListAsync();

        var nextProductDetailCode = await GetNextProductDetailCodeAsync();
        var createdProducts = CreateProductDetails(updateOrderDTO, nextProductDetailCode, oldProductDetails);
        var productDetails = createdProducts.ProductDetails;
        var products = productDetails.SelectMany(detail => detail.Products).ToList();
        var exchangeRate = await GetOrderExchangeRateAsync();
        var totals = CalculateCosts(createdProducts.ProductCosts, updateOrderDTO.PurchaseCurrencyId, exchangeRate, updateOrderDTO.SupplierShippingCostUsd);
        _context.Products.RemoveRange(order.Products);

        var reusedProductDetailIds = productDetails
            .Where(productDetail => productDetail.Id != 0)
            .Select(productDetail => productDetail.Id)
            .ToHashSet();

        var removedProductDetails = oldProductDetails
            .Where(productDetail => !reusedProductDetailIds.Contains(productDetail.Id))
            .ToList();

        _context.ProductDetails.RemoveRange(removedProductDetails);

        order.PurchaseDate = updateOrderDTO.PurchaseDate.NormalizeToUtc() ?? order.PurchaseDate;
        order.SupplierId = updateOrderDTO.SupplierId;
        order.PurchaseCurrencyId = updateOrderDTO.PurchaseCurrencyId;
        order.AmountUsd = totals.AmountUsd;
        order.ExchangeRate = exchangeRate;
        order.MerchandiseTotalNio = totals.MerchandiseTotalNio;
        order.ReceivedAmountNio = 0;
        order.SupplierShippingCostUsd = updateOrderDTO.SupplierShippingCostUsd;
        order.TotalCostNio = totals.TotalCostNio;
        order.Comments = updateOrderDTO.Comments;
        order.Products = products;

        await SyncSupplierPaymentMovementAsync(order);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OrderTrackingNumberDTO>> AddTrackingNumbersAsync(
        int orderId,
        IEnumerable<CreateOrderTrackingNumberDTO> createTrackingDTOs)
    {
        await EnsureOrderExistsAsync(orderId);

        var trackingItems = createTrackingDTOs.ToList();
        if (trackingItems.Count == 0)
        {
            throw new AppBadRequestException("Debe enviar al menos un tracking.");
        }

        foreach (var trackingItem in trackingItems)
        {
            NormalizeTrackingFields(trackingItem);
            await EnsureShippingCompanyExistsAsync(trackingItem.ShippingCompanyId);
            await EnsureProductReceiptExistsAsync(trackingItem.ProductReceiptId);
            await EnsureTrackingNumberIsUniqueAsync(trackingItem.TrackingNumber);
        }

        var duplicatedRequestTracking = trackingItems
            .GroupBy(item => item.TrackingNumber, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedRequestTracking != null)
        {
            throw new AppBadRequestException("No puede enviar números de tracking duplicados en la misma solicitud.");
        }

        var trackingNumbers = trackingItems
            .Select(trackingItem =>
            {
                var trackingNumber = _mapper.Map<OrderTrackingNumber>(trackingItem);
                trackingNumber.OrderId = orderId;
                return trackingNumber;
            })
            .ToList();

        await _context.OrderTrackingNumbers.AddRangeAsync(trackingNumbers);
        await _context.SaveChangesAsync();

        var createdTrackingNumbers = await _context.OrderTrackingNumbers
            .Include(tracking => tracking.ShippingCompany)
            .Where(tracking => trackingNumbers.Select(created => created.Id).Contains(tracking.Id))
            .OrderBy(tracking => tracking.Id)
            .ToListAsync();

        return _mapper.Map<List<OrderTrackingNumberDTO>>(createdTrackingNumbers);
    }

    public async Task<OrderTrackingNumberDTO> UpdateTrackingNumberAsync(
        int orderId,
        int trackingId,
        UpdateOrderTrackingNumberDTO updateTrackingDTO)
    {
        var trackingNumber = await _context.OrderTrackingNumbers
            .Include(tracking => tracking.ShippingCompany)
            .FirstOrDefaultAsync(tracking => tracking.Id == trackingId && tracking.OrderId == orderId)
            ?? throw new AppNotFoundException($"El tracking con id '{trackingId}' no existe para la orden '{orderId}'.");

        NormalizeTrackingFields(updateTrackingDTO);
        await EnsureShippingCompanyExistsAsync(updateTrackingDTO.ShippingCompanyId);
        await EnsureProductReceiptExistsAsync(updateTrackingDTO.ProductReceiptId);
        await EnsureTrackingNumberIsUniqueAsync(updateTrackingDTO.TrackingNumber, trackingId);

        _mapper.Map(updateTrackingDTO, trackingNumber);

        await _context.SaveChangesAsync();

        trackingNumber = await _context.OrderTrackingNumbers
            .Include(tracking => tracking.ShippingCompany)
            .FirstAsync(tracking => tracking.Id == trackingId);

        return _mapper.Map<OrderTrackingNumberDTO>(trackingNumber);
    }

    public async Task DeleteTrackingNumberAsync(int orderId, int trackingId)
    {
        var trackingNumber = await _context.OrderTrackingNumbers
            .FirstOrDefaultAsync(tracking => tracking.Id == trackingId && tracking.OrderId == orderId)
            ?? throw new AppNotFoundException($"El tracking con id '{trackingId}' no existe para la orden '{orderId}'.");

        _context.OrderTrackingNumbers.Remove(trackingNumber);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OrderDTO>> GetAllAsync()
    {
        var orders = await _context.Orders
            .Include(order => order.Supplier)
            .Include(order => order.OrderStatus)
            .Include(order => order.Products)
                .ThenInclude(product => product.ProductDetail)
                    .ThenInclude(productDetail => productDetail!.Subcategory)
            .Include(order => order.Products)
                .ThenInclude(product => product.Size)
            .OrderByDescending(order => order.PurchaseDate)
            .ToListAsync();

        return orders.Select(MapOrderDto).ToList();
    }

    public async Task<OrderDTO> GetByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(order => order.Supplier)
            .Include(order => order.OrderStatus)
            .Include(order => order.Products)
                .ThenInclude(product => product.ProductDetail)
                    .ThenInclude(productDetail => productDetail!.Subcategory)
            .Include(order => order.Products)
                .ThenInclude(product => product.Size)
            .FirstOrDefaultAsync(order => order.Id == id)
            ?? throw new AppNotFoundException($"La orden con id '{id}' no existe.");

        return MapOrderDto(order);
    }

    public async Task<IEnumerable<OrderTrackingNumberDTO>> GetTrackingNumbersAsync(int orderId)
    {
        await EnsureOrderExistsAsync(orderId);

        var trackingNumbers = await _context.OrderTrackingNumbers
            .Include(tracking => tracking.ShippingCompany)
            .Where(tracking => tracking.OrderId == orderId)
            .OrderBy(tracking => tracking.Id)
            .ToListAsync();

        return _mapper.Map<List<OrderTrackingNumberDTO>>(trackingNumbers);
    }

    private async Task SyncSupplierPaymentMovementAsync(Order order)
    {
        var financialMovement = await _context.FinancialMovements
            .FirstOrDefaultAsync(movement =>
                movement.OrderId == order.Id &&
                movement.FinancialMovementTypeId == (int)FinancialMovementTypeOption.SupplierPayment);

        if (order.TotalCostNio <= 0)
        {
            if (financialMovement is not null)
            {
                _context.FinancialMovements.Remove(financialMovement);
            }

            return;
        }

        if (financialMovement is null)
        {
            await _context.FinancialMovements.AddAsync(CreateSupplierPaymentMovement(order));
            return;
        }

        financialMovement.MovementDate = order.PurchaseDate;
        financialMovement.Amount = order.TotalCostNio;
        financialMovement.ExchangeRate = order.ExchangeRate;
        financialMovement.Description = CreateSupplierPaymentDescription(order);
        financialMovement.Comments = order.Comments;
    }

    private static FinancialMovement CreateSupplierPaymentMovement(Order order)
    {
        return new FinancialMovement
        {
            Description = CreateSupplierPaymentDescription(order),
            MovementDate = order.PurchaseDate,
            MovementDirectionId = (int)MovementDirectionOptions.Out,
            FinancialMovementTypeId = (int)FinancialMovementTypeOption.SupplierPayment,
            Order = order,
            Amount = order.TotalCostNio,
            ExchangeRate = order.ExchangeRate,
            Comments = order.Comments
        };
    }

    private static string CreateSupplierPaymentDescription(Order order)
    {
        return order.Id == 0
            ? $"Pago a proveedor por compra. Proveedor #{order.SupplierId}."
            : $"Pago a proveedor por orden #{order.Id}.";
    }


    /// <summary>
    /// Checks if all foreign keys exists (supplierId, subcategoryId, sizeId, etc)
    /// </summary>
    private async Task ValidateOrderRequestAsync(CreateOrderDTO orderDTO)
    {
        await EnsureSupplierExistsAsync(orderDTO.SupplierId);


        var subcategoryIds = orderDTO.ProductDetails
            .Select(productDetail => productDetail.SubcategoryId)
            .Distinct()
            .ToList();

        var existingSubcategoryIds = await _context.Subcategories
            .Where(subcategory => subcategoryIds.Contains(subcategory.Id))
            .Select(subcategory => subcategory.Id)
            .ToListAsync();

        var missingSubcategoryId = subcategoryIds.FirstOrDefault(id => !existingSubcategoryIds.Contains(id));
        if (missingSubcategoryId != 0)
        {
            throw new AppNotFoundException($"La subcategoría con id '{missingSubcategoryId}' no existe.");
        }

        var sizeIds = orderDTO.ProductDetails
            .SelectMany(productDetail => productDetail.Variants)
            .Select(variant => variant.SizeId)
            .Distinct()
            .ToList();

        var existingSizeIds = await _context.Sizes
            .Where(size => sizeIds.Contains(size.Id))
            .Select(size => size.Id)
            .ToListAsync();

        var missingSizeId = sizeIds.FirstOrDefault(id => !existingSizeIds.Contains(id));
        if (missingSizeId != 0)
        {
            throw new AppNotFoundException($"La talla con id '{missingSizeId}' no existe.");
        }

        foreach (var productDetail in orderDTO.ProductDetails)
        {
            if (productDetail.Variants.Count == 0)
            {
                throw new AppBadRequestException("Cada producto debe tener al menos una variante.");
            }

            var duplicatedVariant = productDetail.Variants
                .GroupBy(variant => new
                {
                    variant.SizeId,
                    Color = variant.Color.NormalizeOptional()?.ToLower()
                })
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicatedVariant != null)
            {
                throw new AppBadRequestException("No puede enviar variantes duplicadas para el mismo producto.");
            }
        }
    }

    private static CreatedProductDetails CreateProductDetails(CreateOrderDTO orderDTO, int nextProductDetailCode, List<ProductDetail>? reusableProductDetails = null)
    {
        var reusableDetails = reusableProductDetails?.ToList();
        var productDetails = new List<ProductDetail>();
        var productCosts = new List<ProductPurchaseCost>();

        foreach (var productDetailDTO in orderDTO.ProductDetails)
        {
            var productDetail = reusableDetails is null
                ? null
                : TakeReusableProductDetail(productDetailDTO, reusableDetails);

            productDetail ??= new ProductDetail
            {
                Code = nextProductDetailCode++,
                SupplierProductCode = productDetailDTO.SupplierProductCode,
                Name = productDetailDTO.Name
            };

            productDetail.SupplierProductCode = productDetailDTO.SupplierProductCode;
            productDetail.Name = productDetailDTO.Name;
            productDetail.SubcategoryId = productDetailDTO.SubcategoryId;
            productDetail.Products = [];

            foreach (var variant in productDetailDTO.Variants)
            {
                var product = new Product
                {
                    ProductDetail = productDetail,
                    SizeId = variant.SizeId,
                    Color = variant.Color.NormalizeOptional(),
                    Quantity = variant.Quantity,
                    ReceivedQuantity = 0,
                    AvailableQuantity = 0,
                    ReservedQuantity = 0,
                    SalePrice = variant.SalePrice
                };

                productDetail.Products.Add(product);
                productCosts.Add(new ProductPurchaseCost(product, variant.UnitCost));
            }

            productDetails.Add(productDetail);
        }

        return new CreatedProductDetails(productDetails, productCosts);
    }

    private static ProductDetail? TakeReusableProductDetail(CreateOrderProductDetailDTO productDetailDTO, List<ProductDetail> reusableProductDetails)
    {
        if (!productDetailDTO.Id.HasValue)
        {
            return null;
        }

        var reusableProductDetail = reusableProductDetails.FirstOrDefault(productDetail =>
            productDetail.Id == productDetailDTO.Id.Value)
            ?? throw new AppBadRequestException($"El producto detalle con id '{productDetailDTO.Id.Value}' no pertenece a la orden.");

        reusableProductDetails.Remove(reusableProductDetail);

        return reusableProductDetail;
    }

    private static OrderTotals CalculateCosts(List<ProductPurchaseCost> productCosts, int purchaseCurrencyId, decimal exchangeRate, decimal supplierShippingCostUsd)
    {
        var isUsdPurchase = purchaseCurrencyId == (int)PurchaseCurrencyOption.Usd;
        var merchandiseTotalInPurchaseCurrency = Math.Round(
            productCosts.Sum(item => item.Product.Quantity * item.UnitCostInPurchaseCurrency),
            2);
        var amountUsd = isUsdPurchase
            ? merchandiseTotalInPurchaseCurrency
            : Math.Round(merchandiseTotalInPurchaseCurrency / exchangeRate, 2);
        var merchandiseTotalNio = isUsdPurchase
            ? Math.Round(merchandiseTotalInPurchaseCurrency * exchangeRate, 2)
            : merchandiseTotalInPurchaseCurrency;
        var supplierShippingCostNio = Math.Round(supplierShippingCostUsd * exchangeRate, 2);

        var merchandiseCostNioByLine = productCosts
            .Select(item => CalculateLineMerchandiseCostNio(item, isUsdPurchase, exchangeRate))
            .ToList();

        // Distributes the extra (or missing) decimals when converting to nio
        var merchandiseAllocations = AllocateAmount(merchandiseTotalNio, merchandiseCostNioByLine);
        // Distributes the shipping cost using the cost as weight
        var shippingAllocations = AllocateAmount(supplierShippingCostNio, merchandiseCostNioByLine);

        for (var index = 0; index < productCosts.Count; index++)
        {
            var product = productCosts[index].Product;
            product.MerchandiseTotalCostNio = merchandiseAllocations[index];
            product.AllocatedShippingCostNio = shippingAllocations[index];
            product.TotalCostNio = product.MerchandiseTotalCostNio + product.AllocatedShippingCostNio;
            product.UnitCostNio = Math.Round(product.TotalCostNio / product.Quantity, 6);
            product.UnitCostUsd = exchangeRate == 0
                ? 0
                : Math.Round(product.UnitCostNio / exchangeRate, 2);
        }

        return new OrderTotals(
            amountUsd,
            merchandiseTotalNio,
            merchandiseTotalNio + supplierShippingCostNio);
    }

    private static decimal CalculateLineMerchandiseCostNio(
        ProductPurchaseCost productCost,
        bool isUsdPurchase,
        decimal exchangeRate)
    {
        var product = productCost.Product;

        if (isUsdPurchase)
        {
            return product.Quantity * productCost.UnitCostInPurchaseCurrency * exchangeRate;
        }

        return product.Quantity * productCost.UnitCostInPurchaseCurrency;
    }
    private static List<decimal> AllocateAmount(decimal total, List<decimal> weights)
    {
        if(total == 0)
        {
            return weights.Select(x => x * 0m).ToList();
        }

        var totalWeight = weights.Sum();
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

    private async Task<decimal> GetOrderExchangeRateAsync()
    {
        var exchangeRate = await _context.DollarExchangeRates
            .Where(rate => rate.Enabled)
            .OrderByDescending(rate => rate.StartDate)
            .Select(rate => (decimal?)rate.BankRate)
            .FirstOrDefaultAsync();

        return exchangeRate
            ?? throw new AppBadRequestException("Debe existir una tasa de cambio bancaria habilitada para registrar compras.");
    }

    /// <summary>
    /// Get next consecutive store code
    /// </summary>
    private async Task<int> GetNextProductDetailCodeAsync()
    {
        var maxCode = await _context.ProductDetails
            .MaxAsync(productDetail => (int?)productDetail.Code) ?? 0;

        return maxCode + 1;
    }

    /// <summary>
    /// Checks if order already have received products. If true, it can't be changed.
    /// </summary>
    private static async Task EnsureOrderProductsCanBeReplacedAsync(Order order)
    {
        if (order.Products.Any(product =>
            product.ReceivedQuantity > 0 ||
            product.AvailableQuantity > 0 ||
            product.ReservedQuantity > 0))
        {
            throw new AppBadRequestException("No se puede modificar productos de una orden que ya tiene inventario recibido o reservado.");
        }
    }

    private OrderDTO MapOrderDto(Order order)
    {
        var orderDto = _mapper.Map<OrderDTO>(order);
        orderDto.PurchaseCurrencyName = order.PurchaseCurrencyId == (int)PurchaseCurrencyOption.Usd ? "USD" : "NIO";

        orderDto.ProductDetails = order.Products
            .Where(product => product.ProductDetail != null)
            .GroupBy(product => product.ProductDetail!)
            .Select(group => new OrderProductDetailDTO
            {
                Id = group.Key.Id,
                SupplierProductCode = group.Key.SupplierProductCode,
                Code = group.Key.Code,
                Name = group.Key.Name,
                SubcategoryId = group.Key.SubcategoryId,
                SubcategoryName = group.Key.Subcategory?.Name,
                Variants = group
                    .OrderBy(product => product.Size != null ? product.Size.DisplayOrder : 0)
                    .ThenBy(product => product.Color)
                    .Select(product => new OrderProductVariantDTO
                    {
                        Id = product.Id,
                        SizeId = product.SizeId,
                        SizeName = product.Size?.Name,
                        Color = product.Color,
                        Quantity = product.Quantity,
                        ReceivedQuantity = product.ReceivedQuantity,
                        AvailableQuantity = product.AvailableQuantity,
                        ReservedQuantity = product.ReservedQuantity,
                        UnitCostUsd = product.UnitCostUsd,
                        MerchandiseTotalCostNio = product.MerchandiseTotalCostNio,
                        AllocatedShippingCostNio = product.AllocatedShippingCostNio,
                        TotalCostNio = product.TotalCostNio,
                        UnitCostNio = product.UnitCostNio,
                        SalePrice = product.SalePrice
                    })
                    .ToList()
            })
            .OrderBy(productDetail => productDetail.Code)
            .ToList();

        return orderDto;
    }

    private async Task EnsureSupplierExistsAsync(int supplierId)
    {
        var exists = await _context.Suppliers.AnyAsync(supplier => supplier.Id == supplierId);

        if (!exists)
        {
            throw new AppNotFoundException($"El proveedor con id '{supplierId}' no existe.");
        }
    }

    private async Task EnsureOrderExistsAsync(int orderId)
    {
        var exists = await _context.Orders.AnyAsync(order => order.Id == orderId);

        if (!exists)
        {
            throw new AppNotFoundException($"La orden con id '{orderId}' no existe.");
        }
    }

    private async Task EnsureShippingCompanyExistsAsync(int shippingCompanyId)
    {
        var exists = await _context.ShippingCompanies.AnyAsync(shippingCompany => shippingCompany.Id == shippingCompanyId);

        if (!exists)
        {
            throw new AppNotFoundException($"La empresa de envío con id '{shippingCompanyId}' no existe.");
        }
    }

    private async Task EnsureProductReceiptExistsAsync(int? productReceiptId)
    {
        if (!productReceiptId.HasValue)
        {
            return;
        }

        var exists = await _context.ProductReceipts.AnyAsync(productReceipt => productReceipt.Id == productReceiptId.Value);

        if (!exists)
        {
            throw new AppNotFoundException($"La recepción de productos con id '{productReceiptId.Value}' no existe.");
        }
    }

    private async Task EnsureTrackingNumberIsUniqueAsync(string trackingNumber, int? trackingId = null)
    {
        var exists = await _context.OrderTrackingNumbers.AnyAsync(tracking =>
            tracking.Id != trackingId &&
            tracking.TrackingNumber.ToLower() == trackingNumber.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe un tracking con ese número.");
        }
    }

    private static void NormalizeOrderFields(CreateOrderDTO orderDTO)
    {
        orderDTO.Comments = orderDTO.Comments.NormalizeOptional();
        orderDTO.ProductDetails ??= [];

        foreach (var productDetail in orderDTO.ProductDetails)
        {
            productDetail.SupplierProductCode = productDetail.SupplierProductCode.NormalizeRequired("Código de proveedor");
            productDetail.Name = productDetail.Name.NormalizeRequired("Nombre");

            foreach (var variant in productDetail.Variants)
            {
                variant.Color = variant.Color.NormalizeOptional();
            }
        }
    }

    private static void NormalizeTrackingFields(CreateOrderTrackingNumberDTO trackingDTO)
    {
        trackingDTO.TrackingNumber = trackingDTO.TrackingNumber.NormalizeRequired("Número de tracking");
    }

    private sealed record CreatedProductDetails(
        List<ProductDetail> ProductDetails,
        List<ProductPurchaseCost> ProductCosts);

    private sealed record ProductPurchaseCost(
        Product Product,
        decimal UnitCostInPurchaseCurrency);

    private sealed record OrderTotals(
        decimal AmountUsd,
        decimal MerchandiseTotalNio,
        decimal TotalCostNio);
}
