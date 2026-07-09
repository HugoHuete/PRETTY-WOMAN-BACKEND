using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class ProductService(IApplicationDbContext context) : IProductService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<PaginatedResult<ProductDetailDTO>> GetAllAsync(ProductQueryDTO query)
    {
        NormalizePagination(query);

        var productDetailsQuery = _context.ProductDetails
            .AsNoTracking()
            .AsQueryable();

        productDetailsQuery = ApplyProductDetailFilters(productDetailsQuery, query);

        var totalCount = await productDetailsQuery.CountAsync();
        var productDetails = await productDetailsQuery
            .Include(productDetail => productDetail.Subcategory)
                .ThenInclude(subcategory => subcategory!.Category)
            .Include(productDetail => productDetail.ProductImages)
            .Include(productDetail => productDetail.DiscountCampaignProducts)
                .ThenInclude(discount => discount.DiscountCampaign)
            .Include(productDetail => productDetail.Products)
                .ThenInclude(product => product.Size)
                    .ThenInclude(size => size!.SizeGroup)
            .OrderBy(productDetail => productDetail.Name)
            .ThenBy(productDetail => productDetail.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var items = productDetails
            .Select(productDetail => MapProductDetail(productDetail, query, now))
            .ToList();

        return new PaginatedResult<ProductDetailDTO>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDetailDTO> GetByIdAsync(int id)
    {
        var productDetail = await _context.ProductDetails
            .AsNoTracking()
            .Include(productDetail => productDetail.Subcategory)
                .ThenInclude(subcategory => subcategory!.Category)
            .Include(productDetail => productDetail.ProductImages)
            .Include(productDetail => productDetail.DiscountCampaignProducts)
                .ThenInclude(discount => discount.DiscountCampaign)
            .Include(productDetail => productDetail.Products)
                .ThenInclude(product => product.Size)
                    .ThenInclude(size => size!.SizeGroup)
            .FirstOrDefaultAsync(productDetail => productDetail.Id == id)
            ?? throw new AppNotFoundException($"El producto con id '{id}' no existe.");

        return MapProductDetail(productDetail, new ProductQueryDTO(), DateTime.UtcNow);
    }

    private static IQueryable<ProductDetail> ApplyProductDetailFilters(IQueryable<ProductDetail> query, ProductQueryDTO filters)
    {
        if (filters.Code.HasValue)
        {
            query = query.Where(productDetail => productDetail.Code == filters.Code.Value);
        }

        if (filters.DiscountCampaignId.HasValue)
        {
            query = query.Where(productDetail => productDetail.DiscountCampaignProducts
                .Any(discount => discount.DiscountCampaignId == filters.DiscountCampaignId.Value));
        }

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(productDetail =>
                productDetail.Subcategory != null &&
                productDetail.Subcategory.CategoryId == filters.CategoryId.Value);
        }

        if (filters.SubcategoryId.HasValue)
        {
            query = query.Where(productDetail => productDetail.SubcategoryId == filters.SubcategoryId.Value);
        }

        if (filters.SizeId.HasValue || filters.Availability.HasValue)
        {
            query = query.Where(productDetail => productDetail.Products.Any(product =>
                (!filters.SizeId.HasValue || product.SizeId == filters.SizeId.Value) &&
                (!filters.Availability.HasValue ||
                    (filters.Availability.Value == ProductAvailabilityFilter.Available && product.AvailableQuantity > 0) ||
                    (filters.Availability.Value == ProductAvailabilityFilter.Reserved && product.ReservedQuantity > 0) ||
                    (filters.Availability.Value == ProductAvailabilityFilter.Unavailable && product.UnavailableQuantity > 0))));
        }

        return query;
    }

    private static ProductDetailDTO MapProductDetail(ProductDetail productDetail, ProductQueryDTO query, DateTime now)
    {
        return new ProductDetailDTO
        {
            Id = productDetail.Id,
            SupplierProductCode = productDetail.SupplierProductCode,
            Code = productDetail.Code,
            Name = productDetail.Name,
            SubcategoryId = productDetail.SubcategoryId,
            SubcategoryName = productDetail.Subcategory?.Name,
            CategoryId = productDetail.Subcategory?.CategoryId,
            CategoryName = productDetail.Subcategory?.Category?.Name,
            PrimaryImageUrl = productDetail.ProductImages
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder)
                .Select(image => image.ImageUrl)
                .FirstOrDefault(),
            Products = productDetail.Products
                .Where(product =>
                    (!query.SizeId.HasValue || product.SizeId == query.SizeId.Value) &&
                    (!query.Availability.HasValue ||
                        (query.Availability.Value == ProductAvailabilityFilter.Available && product.AvailableQuantity > 0) ||
                        (query.Availability.Value == ProductAvailabilityFilter.Reserved && product.ReservedQuantity > 0) ||
                        (query.Availability.Value == ProductAvailabilityFilter.Unavailable && product.UnavailableQuantity > 0)))
                .OrderBy(product => product.Size?.DisplayOrder ?? 0)
                .ThenBy(product => product.Color)
                .Select(product => MapProductVariant(productDetail, product, now))
                .ToList()
        };
    }

    private static ProductVariantDTO MapProductVariant(ProductDetail productDetail, Product product, DateTime now)
    {
        var discount = GetBestActiveDiscount(productDetail, product.SalePrice, now);

        return new ProductVariantDTO
        {
            Id = product.Id,
            SizeId = product.SizeId,
            SizeName = product.Size?.Name,
            SizeGroupId = product.Size?.SizeGroupId,
            SizeGroupName = product.Size?.SizeGroup?.Name,
            Color = product.Color,
            Quantity = product.Quantity,
            ReceivedQuantity = product.ReceivedQuantity,
            AvailableQuantity = product.AvailableQuantity,
            ReservedQuantity = product.ReservedQuantity,
            UnavailableQuantity = product.UnavailableQuantity,
            SalePrice = product.SalePrice,
            DiscountedSalePrice = discount?.DiscountedSalePrice,
            DiscountCampaignId = discount?.CampaignId,
            DiscountCampaignName = discount?.CampaignName
        };
    }

    private static ActiveDiscountDTO? GetBestActiveDiscount(ProductDetail productDetail, decimal salePrice, DateTime now)
    {
        return productDetail.DiscountCampaignProducts
            .Where(discount =>
                discount.DiscountCampaign is { Enabled: true } &&
                discount.DiscountCampaign.StartDate <= now &&
                discount.DiscountCampaign.EndDate >= now)
            .Select(discount => new ActiveDiscountDTO(
                discount.DiscountCampaignId,
                discount.DiscountCampaign!.Name,
                CalculateDiscountedPrice(salePrice, discount.DiscountTypeId, discount.DiscountValue)))
            .Where(discount => discount.DiscountedSalePrice < salePrice)
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
