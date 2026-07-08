using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.DTOs.Discounts;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class DiscountCampaignService(IApplicationDbContext context) : IDiscountCampaignService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<int> CreateAsync(CreateDiscountCampaignDTO createDiscountCampaignDTO)
    {
        NormalizeAndValidateCampaign(createDiscountCampaignDTO);
        await ValidateProductDetailsAsync(createDiscountCampaignDTO.Products);
        await EnsureNameIsUniqueAsync(createDiscountCampaignDTO.Name);

        var discountCampaign = new DiscountCampaign
        {
            Name = createDiscountCampaignDTO.Name,
            StartDate = createDiscountCampaignDTO.StartDate,
            EndDate = createDiscountCampaignDTO.EndDate,
            Enabled = createDiscountCampaignDTO.Enabled,
            DiscountCampaignProducts = createDiscountCampaignDTO.Products
                .Select(product => new DiscountCampaignProduct
                {
                    ProductDetailId = product.ProductDetailId,
                    DiscountTypeId = product.DiscountTypeId,
                    DiscountValue = product.DiscountValue
                })
                .ToList()
        };

        await _context.DiscountCampaigns.AddAsync(discountCampaign);
        await _context.SaveChangesAsync();

        return discountCampaign.Id;
    }

    public async Task UpdateAsync(int id, UpdateDiscountCampaignDTO updateDiscountCampaignDTO)
    {
        var discountCampaign = await _context.DiscountCampaigns
            .Include(campaign => campaign.DiscountCampaignProducts)
            .FirstOrDefaultAsync(campaign => campaign.Id == id)
            ?? throw new AppNotFoundException($"La campania de descuento con id '{id}' no existe.");

        NormalizeAndValidateCampaign(updateDiscountCampaignDTO);
        await ValidateProductDetailsAsync(updateDiscountCampaignDTO.Products);
        await EnsureNameIsUniqueAsync(updateDiscountCampaignDTO.Name, id);

        discountCampaign.Name = updateDiscountCampaignDTO.Name;
        discountCampaign.StartDate = updateDiscountCampaignDTO.StartDate;
        discountCampaign.EndDate = updateDiscountCampaignDTO.EndDate;
        discountCampaign.Enabled = updateDiscountCampaignDTO.Enabled;

        var existingProductsByProductDetailId = discountCampaign.DiscountCampaignProducts
            .ToDictionary(product => product.ProductDetailId);
        var requestedProductDetailIds = updateDiscountCampaignDTO.Products
            .Select(product => product.ProductDetailId)
            .ToHashSet();

        var productsToRemove = discountCampaign.DiscountCampaignProducts
            .Where(product => !requestedProductDetailIds.Contains(product.ProductDetailId))
            .ToList();

        _context.DiscountCampaignProducts.RemoveRange(productsToRemove);

        foreach (var product in updateDiscountCampaignDTO.Products)
        {
            if (existingProductsByProductDetailId.TryGetValue(product.ProductDetailId, out var existingProduct))
            {
                existingProduct.DiscountTypeId = product.DiscountTypeId;
                existingProduct.DiscountValue = product.DiscountValue;
                continue;
            }

            discountCampaign.DiscountCampaignProducts.Add(new DiscountCampaignProduct
            {
                ProductDetailId = product.ProductDetailId,
                DiscountTypeId = product.DiscountTypeId,
                DiscountValue = product.DiscountValue
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task DisableAsync(int id)
    {
        var discountCampaign = await _context.DiscountCampaigns.FirstOrDefaultAsync(campaign => campaign.Id == id)
            ?? throw new AppNotFoundException($"La campania de descuento con id '{id}' no existe.");

        discountCampaign.Enabled = false;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DiscountCampaignDTO>> GetAllAsync(bool? enabled = null)
    {
        var discountCampaigns = await _context.DiscountCampaigns
            .AsNoTracking()
            .Where(campaign => !enabled.HasValue || campaign.Enabled == enabled.Value)
            .OrderByDescending(campaign => campaign.StartDate)
            .ThenBy(campaign => campaign.Name)
            .Select(campaign => new DiscountCampaignDTO
            {
                Id = campaign.Id,
                Name = campaign.Name,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                Enabled = campaign.Enabled,
                CreatedAt = campaign.CreatedAt,
                UpdatedAt = campaign.UpdatedAt,
                CreatedById = campaign.CreatedById,
                UpdatedById = campaign.UpdatedById,
                Products = campaign.DiscountCampaignProducts
                    .OrderBy(product => product.ProductDetail != null ? product.ProductDetail.Name : string.Empty)
                    .Select(product => new DiscountCampaignProductDTO
                    {
                        Id = product.Id,
                        ProductDetailId = product.ProductDetailId,
                        ProductName = product.ProductDetail != null ? product.ProductDetail.Name : null,
                        ProductCode = product.ProductDetail != null ? product.ProductDetail.Code : null,
                        DiscountTypeId = product.DiscountTypeId,
                        DiscountTypeName = product.DiscountType != null ? product.DiscountType.Name : null,
                        DiscountValue = product.DiscountValue
                    })
                    .ToList()
            })
            .ToListAsync();

        return discountCampaigns;
    }

    public async Task<DiscountCampaignDTO> GetByIdAsync(int id)
    {
        var discountCampaign = await _context.DiscountCampaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == id)
            .Select(campaign => new DiscountCampaignDTO
            {
                Id = campaign.Id,
                Name = campaign.Name,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                Enabled = campaign.Enabled,
                CreatedAt = campaign.CreatedAt,
                UpdatedAt = campaign.UpdatedAt,
                CreatedById = campaign.CreatedById,
                UpdatedById = campaign.UpdatedById,
                Products = campaign.DiscountCampaignProducts
                    .OrderBy(product => product.ProductDetail != null ? product.ProductDetail.Name : string.Empty)
                    .Select(product => new DiscountCampaignProductDTO
                    {
                        Id = product.Id,
                        ProductDetailId = product.ProductDetailId,
                        ProductName = product.ProductDetail != null ? product.ProductDetail.Name : null,
                        ProductCode = product.ProductDetail != null ? product.ProductDetail.Code : null,
                        DiscountTypeId = product.DiscountTypeId,
                        DiscountTypeName = product.DiscountType != null ? product.DiscountType.Name : null,
                        DiscountValue = product.DiscountValue
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync()
            ?? throw new AppNotFoundException($"La campania de descuento con id '{id}' no existe.");

        return discountCampaign;
    }

    private static void NormalizeAndValidateCampaign(CreateDiscountCampaignDTO discountCampaignDTO)
    {
        discountCampaignDTO.Name = NormalizeAndValidateCampaign(
            discountCampaignDTO.Name,
            discountCampaignDTO.StartDate,
            discountCampaignDTO.EndDate);
    }

    private static void NormalizeAndValidateCampaign(UpdateDiscountCampaignDTO discountCampaignDTO)
    {
        discountCampaignDTO.Name = NormalizeAndValidateCampaign(
            discountCampaignDTO.Name,
            discountCampaignDTO.StartDate,
            discountCampaignDTO.EndDate);
    }

    private static string NormalizeAndValidateCampaign(string name, DateTime startDate, DateTime endDate)
    {
        name = name.NormalizeRequired("Nombre de la campania de descuento");

        if (endDate <= startDate)
        {
            throw new AppBadRequestException("La fecha final de la campania debe ser mayor que la fecha inicial.");
        }

        return name;
    }

    private async Task ValidateProductDetailsAsync(IReadOnlyCollection<CreateDiscountCampaignProductDTO> products)
    {
        foreach (var product in products)
        {
            ValidateDiscountValue(product);
        }

        var repeatedProductDetailId = products
            .GroupBy(product => product.ProductDetailId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();

        if (repeatedProductDetailId > 0)
        {
            throw new AppBadRequestException($"El producto detalle con id '{repeatedProductDetailId}' esta repetido en la campania.");
        }

        var productDetailIds = products
            .Select(product => product.ProductDetailId)
            .Distinct()
            .ToList();

        var existingProductDetailIds = await _context.ProductDetails
            .Where(product => productDetailIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync();

        var missingProductDetailId = productDetailIds.Except(existingProductDetailIds).FirstOrDefault();

        if (missingProductDetailId > 0)
        {
            throw new AppNotFoundException($"El producto detalle con id '{missingProductDetailId}' no existe.");
        }

        var discountTypeIds = products
            .Select(product => product.DiscountTypeId)
            .Distinct()
            .ToList();

        var existingDiscountTypeIds = await _context.DiscountTypes
            .Where(discountType => discountTypeIds.Contains(discountType.Id))
            .Select(discountType => discountType.Id)
            .ToListAsync();

        var missingDiscountTypeId = discountTypeIds.Except(existingDiscountTypeIds).FirstOrDefault();

        if (missingDiscountTypeId > 0)
        {
            throw new AppNotFoundException($"El tipo de descuento con id '{missingDiscountTypeId}' no existe.");
        }
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? currentCampaignId = null)
    {
        var exists = await _context.DiscountCampaigns
            .AnyAsync(campaign =>
                (!currentCampaignId.HasValue || campaign.Id != currentCampaignId.Value) &&
                campaign.Name.ToLower() == name.ToLower());

        if (exists)
        {
            throw new AppBadRequestException("Ya existe una campania de descuento con ese nombre.");
        }
    }

    private static void ValidateDiscountValue(CreateDiscountCampaignProductDTO product)
    {
        if (product.DiscountValue <= 0)
        {
            throw new AppBadRequestException("El valor del descuento debe ser mayor que cero.");
        }

        if (product.DiscountTypeId == (int)DiscountTypeOption.Percentage && product.DiscountValue > 100)
        {
            throw new AppBadRequestException("El porcentaje de descuento no puede ser mayor que 100.");
        }
    }
}
