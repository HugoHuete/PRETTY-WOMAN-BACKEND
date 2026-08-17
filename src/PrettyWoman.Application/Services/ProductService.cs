using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class ProductService(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver) : IProductService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<PaginatedResult<ProductDTO>> GetAllAsync(ProductQueryDTO query)
    {
        NormalizePagination(query);

        var productsQuery = _context.Products
            .AsNoTracking()
            .AsQueryable();

        productsQuery = ApplyProductFilters(productsQuery, query);

        var totalCount = await productsQuery.CountAsync();
        var products = await productsQuery
            .Include(product => product.Subcategory)
                .ThenInclude(subcategory => subcategory!.Category)
            .Include(product => product.ProductImages)
                .ThenInclude(productImage => productImage.MediaAsset)
                    .ThenInclude(mediaAsset => mediaAsset!.Variants)
            .Include(product => product.DiscountCampaignProducts)
                .ThenInclude(discount => discount.DiscountCampaign)
            .Include(product => product.DiscountCampaignProducts)
                .ThenInclude(discount => discount.ProductVariant)
            .Include(product => product.ProductVariants)
                .ThenInclude(productVariant => productVariant.Size)
                    .ThenInclude(size => size!.SizeGroup)
            .Include(product => product.ProductVariants)
                .ThenInclude(productVariant => productVariant.DiscountCampaignProducts)
                    .ThenInclude(discount => discount.DiscountCampaign)
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var items = products
            .Select(product => MapProduct(product, query, now))
            .ToList();

        return new PaginatedResult<ProductDTO>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDTO> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(product => product.Subcategory)
                .ThenInclude(subcategory => subcategory!.Category)
            .Include(product => product.ProductImages)
                .ThenInclude(productImage => productImage.MediaAsset)
                    .ThenInclude(mediaAsset => mediaAsset!.Variants)
            .Include(product => product.DiscountCampaignProducts)
                .ThenInclude(discount => discount.DiscountCampaign)
            .Include(product => product.DiscountCampaignProducts)
                .ThenInclude(discount => discount.ProductVariant)
            .Include(product => product.ProductVariants)
                .ThenInclude(productVariant => productVariant.Size)
                    .ThenInclude(size => size!.SizeGroup)
            .Include(product => product.ProductVariants)
                .ThenInclude(productVariant => productVariant.DiscountCampaignProducts)
                    .ThenInclude(discount => discount.DiscountCampaign)
            .FirstOrDefaultAsync(product => product.Id == id)
            ?? throw new AppNotFoundException($"El producto con id '{id}' no existe.");

        return MapProduct(product, new ProductQueryDTO(), DateTime.UtcNow);
    }

    public async Task UpdatePriceAsync(int productId, int productVariantId, UpdateProductPriceDTO request)
    {
        var productVariant = await _context.ProductVariants
            .FirstOrDefaultAsync(productVariant => productVariant.Id == productVariantId && productVariant.ProductId == productId)
            ?? throw new AppNotFoundException($"La variante con id '{productVariantId}' no existe para el producto con id '{productId}'.");

        productVariant.SalePrice = request.SalePrice;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProductInventoryMovementDTO>> GetInventoryMovementsAsync(int productId, int? productVariantId = null)
    {
        var productExists = await _context.Products
            .AsNoTracking()
            .AnyAsync(product => product.Id == productId);

        if (!productExists)
        {
            throw new AppNotFoundException($"El producto con id '{productId}' no existe.");
        }

        if (productVariantId.HasValue)
        {
            var productBelongsToProduct = await _context.ProductVariants
                .AsNoTracking()
                .AnyAsync(productVariant => productVariant.Id == productVariantId.Value && productVariant.ProductId == productId);

            if (!productBelongsToProduct)
            {
                throw new AppNotFoundException($"La variante con id '{productVariantId.Value}' no existe para el producto con id '{productId}'.");
            }
        }

        var movementsQuery = _context.InventoryMovements
            .AsNoTracking()
            .Where(movement => movement.ProductVariant != null && movement.ProductVariant.ProductId == productId);

        if (productVariantId.HasValue)
        {
            movementsQuery = movementsQuery.Where(movement => movement.ProductId == productVariantId.Value);
        }

        return await movementsQuery
            .OrderByDescending(movement => movement.MovementDate)
            .ThenByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.Id)
            .Select(movement => new ProductInventoryMovementDTO
            {
                Id = movement.Id,
                ProductVariantId = movement.ProductId,
                ProductId = movement.ProductVariant!.ProductId,
                ProductName = movement.ProductVariant.Product != null ? movement.ProductVariant.Product.Name : null,
                ProductCode = movement.ProductVariant.Product != null ? movement.ProductVariant.Product.Code : null,
                SizeId = movement.ProductVariant.SizeId,
                SizeName = movement.ProductVariant.Size != null ? movement.ProductVariant.Size.Name : null,
                Variant = movement.ProductVariant.Variant,
                MovementDate = movement.MovementDate,
                InventoryMovementTypeId = movement.InventoryMovementTypeId,
                InventoryMovementTypeName = movement.InventoryMovementType != null ? movement.InventoryMovementType.Name : null,
                FromStockBucketId = movement.FromStockBucketId,
                FromStockBucketName = movement.FromStockBucket != null ? movement.FromStockBucket.Name : null,
                ToStockBucketId = movement.ToStockBucketId,
                ToStockBucketName = movement.ToStockBucket != null ? movement.ToStockBucket.Name : null,
                Quantity = movement.Quantity,
                OrderId = movement.OrderId,
                SaleProductId = movement.SaleProductId,
                ProductHoldId = movement.ProductHoldId,
                ProductInventoryIssueId = movement.ProductInventoryIssueId,
                Comments = movement.Comments,
                CreatedAt = movement.CreatedAt
            })
            .ToListAsync();
    }

    private static IQueryable<Product> ApplyProductFilters(IQueryable<Product> query, ProductQueryDTO filters)
    {
        if (filters.Code.HasValue)
        {
            query = query.Where(product => product.Code == filters.Code.Value);
        }

        if (filters.DiscountCampaignId.HasValue)
        {
            query = query.Where(product => product.DiscountCampaignProducts
                .Any(discount => discount.DiscountCampaignId == filters.DiscountCampaignId.Value) ||
                product.ProductVariants.Any(productVariant => productVariant.DiscountCampaignProducts
                    .Any(discount => discount.DiscountCampaignId == filters.DiscountCampaignId.Value)));
        }

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(product =>
                product.Subcategory != null &&
                product.Subcategory.CategoryId == filters.CategoryId.Value);
        }

        if (filters.SubcategoryId.HasValue)
        {
            query = query.Where(product => product.SubcategoryId == filters.SubcategoryId.Value);
        }

        if (filters.SizeId.HasValue || filters.Availability.HasValue)
        {
            query = query.Where(product => product.ProductVariants.Any(productVariant =>
                (!filters.SizeId.HasValue || productVariant.SizeId == filters.SizeId.Value) &&
                (!filters.Availability.HasValue ||
                    (filters.Availability.Value == ProductAvailabilityFilter.Available && productVariant.AvailableQuantity > 0) ||
                    (filters.Availability.Value == ProductAvailabilityFilter.Reserved && productVariant.ReservedQuantity > 0) ||
                    (filters.Availability.Value == ProductAvailabilityFilter.Unavailable && productVariant.UnavailableQuantity > 0))));
        }

        return query;
    }

    private ProductDTO MapProduct(Product product, ProductQueryDTO query, DateTime now)
    {
        return new ProductDTO
        {
            Id = product.Id,
            SupplierProductCode = product.SupplierProductCode,
            Code = product.Code,
            Name = product.Name,
            SubcategoryId = product.SubcategoryId,
            SubcategoryName = product.Subcategory?.Name,
            CategoryId = product.Subcategory?.CategoryId,
            CategoryName = product.Subcategory?.Category?.Name,
            PrimaryImageUrl = GetPrimaryImageUrl(product),
            Variants = product.ProductVariants
                .Where(productVariant =>
                    (!query.SizeId.HasValue || productVariant.SizeId == query.SizeId.Value) &&
                    (!query.Availability.HasValue ||
                        (query.Availability.Value == ProductAvailabilityFilter.Available && productVariant.AvailableQuantity > 0) ||
                        (query.Availability.Value == ProductAvailabilityFilter.Reserved && productVariant.ReservedQuantity > 0) ||
                        (query.Availability.Value == ProductAvailabilityFilter.Unavailable && productVariant.UnavailableQuantity > 0)))
                .OrderBy(productVariant => productVariant.Size?.DisplayOrder ?? 0)
                .ThenBy(productVariant => productVariant.Variant)
                .Select(productVariant => MapProductVariant(product, productVariant, now))
                .ToList()
        };
    }

    private string? GetPrimaryImageUrl(Product product)
    {
        var primaryImage = product.ProductImages
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.SortOrder)
            .FirstOrDefault();

        var storageKey = primaryImage?.MediaAsset?.Variants
            .FirstOrDefault(variant => variant.Type == MediaVariantType.Web)
            ?.StorageKey;

        return storageKey is null ? null : mediaUrlResolver.GetPublicUrl(storageKey);
    }

    private static ProductVariantDTO MapProductVariant(Product product, ProductVariant productVariant, DateTime now)
    {
        var discount = GetBestActiveDiscount(product, productVariant, now);

        return new ProductVariantDTO
        {
            Id = productVariant.Id,
            SizeId = productVariant.SizeId,
            SizeName = productVariant.Size?.Name,
            SizeGroupId = productVariant.Size?.SizeGroupId,
            SizeGroupName = productVariant.Size?.SizeGroup?.Name,
            Variant = productVariant.Variant,
            Quantity = productVariant.Quantity,
            ReceivedQuantity = productVariant.ReceivedQuantity,
            AvailableQuantity = productVariant.AvailableQuantity,
            ReservedQuantity = productVariant.ReservedQuantity,
            UnavailableQuantity = productVariant.UnavailableQuantity,
            SalePrice = productVariant.SalePrice,
            DiscountedSalePrice = discount?.DiscountedSalePrice,
            DiscountCampaignId = discount?.CampaignId,
            DiscountCampaignName = discount?.CampaignName
        };
    }

    private static ActiveDiscountDTO? GetBestActiveDiscount(Product product, ProductVariant productVariant, DateTime now)
    {
        return product.DiscountCampaignProducts
            .Concat(productVariant.DiscountCampaignProducts)
            .Where(discount =>
                discount.DiscountCampaign is { CancelledAt: null } &&
                discount.DiscountCampaign.StartDate <= now &&
                discount.DiscountCampaign.EndDate >= now)
            .Select(discount => new ActiveDiscountDTO(
                discount.DiscountCampaignId,
                discount.DiscountCampaign!.Name,
                CalculateDiscountedPrice(productVariant.SalePrice, discount.DiscountTypeId, discount.DiscountValue)))
            .Where(discount => discount.DiscountedSalePrice < productVariant.SalePrice)
            .OrderBy(discount => discount.DiscountedSalePrice)
            .ThenBy(discount => discount.CampaignId)
            .FirstOrDefault();
    }

    private static decimal CalculateDiscountedPrice(decimal salePrice, int discountTypeId, decimal discountValue)
    {
        var discountedPrice = (DiscountTypeOption)discountTypeId switch
        {
            DiscountTypeOption.FixedAmount => salePrice - discountValue,
            DiscountTypeOption.Percentage => salePrice * (1 - discountValue / 100),
            DiscountTypeOption.FixedPrice => discountValue,
            _ => salePrice
        };

        return Math.Round(Math.Max(0, discountedPrice), 2, MidpointRounding.AwayFromZero);
    }

    private static void NormalizePagination(ProductQueryDTO query)
    {
        if (query.Page <= 0)
        {
            query.Page = 1;
        }

        if (query.PageSize <= 0)
        {
            query.PageSize = 20;
        }

        if (query.PageSize > 100)
        {
            query.PageSize = 100;
        }
    }

    private sealed record ActiveDiscountDTO(int CampaignId, string CampaignName, decimal DiscountedSalePrice);
}
