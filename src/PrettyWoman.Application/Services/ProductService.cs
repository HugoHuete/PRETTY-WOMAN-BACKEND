using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

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
        var items = await productDetailsQuery
            .OrderBy(productDetail => productDetail.Name)
            .ThenBy(productDetail => productDetail.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(productDetail => new ProductDetailDTO
            {
                Id = productDetail.Id,
                SupplierProductCode = productDetail.SupplierProductCode,
                Code = productDetail.Code,
                Name = productDetail.Name,
                SubcategoryId = productDetail.SubcategoryId,
                SubcategoryName = productDetail.Subcategory != null ? productDetail.Subcategory.Name : null,
                CategoryId = productDetail.Subcategory != null ? productDetail.Subcategory.CategoryId : null,
                CategoryName = productDetail.Subcategory != null && productDetail.Subcategory.Category != null
                    ? productDetail.Subcategory.Category.Name
                    : null,
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
                    .OrderBy(product => product.Size != null ? product.Size.DisplayOrder : 0)
                    .ThenBy(product => product.Color)
                    .Select(product => new ProductVariantDTO
                    {
                        Id = product.Id,
                        SizeId = product.SizeId,
                        SizeName = product.Size != null ? product.Size.Name : null,
                        SizeGroupId = product.Size != null ? product.Size.SizeGroupId : null,
                        SizeGroupName = product.Size != null && product.Size.SizeGroup != null ? product.Size.SizeGroup.Name : null,
                        Color = product.Color,
                        Quantity = product.Quantity,
                        ReceivedQuantity = product.ReceivedQuantity,
                        AvailableQuantity = product.AvailableQuantity,
                        ReservedQuantity = product.ReservedQuantity,
                        UnavailableQuantity = product.UnavailableQuantity,
                        SalePrice = product.SalePrice
                    })
                    .ToList()
            })
            .ToListAsync();

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
            .Where(productDetail => productDetail.Id == id)
            .Select(productDetail => new ProductDetailDTO
            {
                Id = productDetail.Id,
                SupplierProductCode = productDetail.SupplierProductCode,
                Code = productDetail.Code,
                Name = productDetail.Name,
                SubcategoryId = productDetail.SubcategoryId,
                SubcategoryName = productDetail.Subcategory != null ? productDetail.Subcategory.Name : null,
                CategoryId = productDetail.Subcategory != null ? productDetail.Subcategory.CategoryId : null,
                CategoryName = productDetail.Subcategory != null && productDetail.Subcategory.Category != null
                    ? productDetail.Subcategory.Category.Name
                    : null,
                PrimaryImageUrl = productDetail.ProductImages
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.SortOrder)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),
                Products = productDetail.Products
                    .OrderBy(product => product.Size != null ? product.Size.DisplayOrder : 0)
                    .ThenBy(product => product.Color)
                    .Select(product => new ProductVariantDTO
                    {
                        Id = product.Id,
                        SizeId = product.SizeId,
                        SizeName = product.Size != null ? product.Size.Name : null,
                        SizeGroupId = product.Size != null ? product.Size.SizeGroupId : null,
                        SizeGroupName = product.Size != null && product.Size.SizeGroup != null ? product.Size.SizeGroup.Name : null,
                        Color = product.Color,
                        Quantity = product.Quantity,
                        ReceivedQuantity = product.ReceivedQuantity,
                        AvailableQuantity = product.AvailableQuantity,
                        ReservedQuantity = product.ReservedQuantity,
                        UnavailableQuantity = product.UnavailableQuantity,
                        SalePrice = product.SalePrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync()
            ?? throw new AppNotFoundException($"El producto con id '{id}' no existe.");

        return productDetail;
    }

    private static IQueryable<ProductDetail> ApplyProductDetailFilters(IQueryable<ProductDetail> query, ProductQueryDTO filters)
    {
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
}
