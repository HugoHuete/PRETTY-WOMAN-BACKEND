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
        await ValidateProductsAsync(createDiscountCampaignDTO.Products);
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
                    ProductId = product.ProductId,
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
        await ValidateProductsAsync(updateDiscountCampaignDTO.Products);
        await EnsureNameIsUniqueAsync(updateDiscountCampaignDTO.Name, id);

        discountCampaign.Name = updateDiscountCampaignDTO.Name;
        discountCampaign.StartDate = updateDiscountCampaignDTO.StartDate;
        discountCampaign.EndDate = updateDiscountCampaignDTO.EndDate;
        discountCampaign.Enabled = updateDiscountCampaignDTO.Enabled;

        var existingProductsByProductId = discountCampaign.DiscountCampaignProducts
            .ToDictionary(product => product.ProductId);
        var requestedProductIds = updateDiscountCampaignDTO.Products
            .Select(product => product.ProductId)
            .ToHashSet();

        var productsToRemove = discountCampaign.DiscountCampaignProducts
            .Where(product => !requestedProductIds.Contains(product.ProductId))
            .ToList();

        _context.DiscountCampaignProducts.RemoveRange(productsToRemove);

        foreach (var product in updateDiscountCampaignDTO.Products)
        {
            if (existingProductsByProductId.TryGetValue(product.ProductId, out var existingProduct))
            {
                existingProduct.DiscountTypeId = product.DiscountTypeId;
                existingProduct.DiscountValue = product.DiscountValue;
                continue;
            }

            discountCampaign.DiscountCampaignProducts.Add(new DiscountCampaignProduct
            {
                ProductId = product.ProductId,
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
                Products = campaign.DiscountCampaignProducts
                    .OrderBy(product => product.Product != null && product.Product.ProductDetail != null ? product.Product.ProductDetail.Name : string.Empty)
                    .ThenBy(product => product.Product != null && product.Product.Size != null ? product.Product.Size.DisplayOrder : 0)
                    .Select(product => new DiscountCampaignProductDTO
                    {
                        Id = product.Id,
                        ProductId = product.ProductId,
                        ProductName = product.Product != null && product.Product.ProductDetail != null ? product.Product.ProductDetail.Name : null,
                        ProductCode = product.Product != null && product.Product.ProductDetail != null ? product.Product.ProductDetail.Code : null,
                        SizeId = product.Product != null ? product.Product.SizeId : null,
                        SizeName = product.Product != null && product.Product.Size != null ? product.Product.Size.Name : null,
                        Color = product.Product != null ? product.Product.Color : null,
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
                Products = campaign.DiscountCampaignProducts
                    .OrderBy(product => product.Product != null && product.Product.ProductDetail != null ? product.Product.ProductDetail.Name : string.Empty)
                    .ThenBy(product => product.Product != null && product.Product.Size != null ? product.Product.Size.DisplayOrder : 0)
                    .Select(product => new DiscountCampaignProductDTO
                    {
                        Id = product.Id,
                        ProductId = product.ProductId,
                        ProductName = product.Product != null && product.Product.ProductDetail != null ? product.Product.ProductDetail.Name : null,
                        ProductCode = product.Product != null && product.Product.ProductDetail != null ? product.Product.ProductDetail.Code : null,
                        SizeId = product.Product != null ? product.Product.SizeId : null,
                        SizeName = product.Product != null && product.Product.Size != null ? product.Product.Size.Name : null,
                        Color = product.Product != null ? product.Product.Color : null,
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

    private async Task ValidateProductsAsync(IReadOnlyCollection<CreateDiscountCampaignProductDTO> products)
    {
        foreach (var product in products)
        {
            ValidateDiscountValue(product);
        }

        var repeatedProductId = products
            .GroupBy(product => product.ProductId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();

        if (repeatedProductId > 0)
        {
            throw new AppBadRequestException($"El producto con id '{repeatedProductId}' esta repetido en la campania.");
        }

        var productIds = products
            .Select(product => product.ProductId)
            .Distinct()
            .ToList();

        var existingProductIds = await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync();

        var missingProductId = productIds.Except(existingProductIds).FirstOrDefault();

        if (missingProductId > 0)
        {
            throw new AppNotFoundException($"El producto con id '{missingProductId}' no existe.");
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
