using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Discounts;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.Common.Models;
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
        await ValidateProductsAsync(createDiscountCampaignDTO.ProductVariants);
        await EnsureNameIsUniqueAsync(createDiscountCampaignDTO.Name);

        var discountCampaign = new DiscountCampaign
        {
            Name = createDiscountCampaignDTO.Name,
            StartDate = createDiscountCampaignDTO.StartDate,
            EndDate = createDiscountCampaignDTO.EndDate,
            CancelledAt = null,
            DiscountCampaignProducts = createDiscountCampaignDTO.ProductVariants
                .Select(productVariant => new DiscountCampaignProduct
                {
                    ProductId = productVariant.ProductId,
                    DiscountTypeId = productVariant.DiscountTypeId,
                    DiscountValue = productVariant.DiscountValue
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
        await ValidateProductsAsync(updateDiscountCampaignDTO.ProductVariants);
        await EnsureNameIsUniqueAsync(updateDiscountCampaignDTO.Name, id);

        discountCampaign.Name = updateDiscountCampaignDTO.Name;
        discountCampaign.StartDate = updateDiscountCampaignDTO.StartDate;
        discountCampaign.EndDate = updateDiscountCampaignDTO.EndDate;

        var existingProductsByProductId = discountCampaign.DiscountCampaignProducts
            .ToDictionary(productVariant => productVariant.ProductId);
        var requestedProductIds = updateDiscountCampaignDTO.ProductVariants
            .Select(productVariant => productVariant.ProductId)
            .ToHashSet();

        var productsToRemove = discountCampaign.DiscountCampaignProducts
            .Where(productVariant => !requestedProductIds.Contains(productVariant.ProductId))
            .ToList();

        _context.DiscountCampaignProducts.RemoveRange(productsToRemove);

        foreach (var productVariant in updateDiscountCampaignDTO.ProductVariants)
        {
            if (existingProductsByProductId.TryGetValue(productVariant.ProductId, out var existingProduct))
            {
                existingProduct.DiscountTypeId = productVariant.DiscountTypeId;
                existingProduct.DiscountValue = productVariant.DiscountValue;
                continue;
            }

            discountCampaign.DiscountCampaignProducts.Add(new DiscountCampaignProduct
            {
                ProductId = productVariant.ProductId,
                DiscountTypeId = productVariant.DiscountTypeId,
                DiscountValue = productVariant.DiscountValue
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int id)
    {
        var discountCampaign = await _context.DiscountCampaigns.FirstOrDefaultAsync(campaign => campaign.Id == id)
            ?? throw new AppNotFoundException($"La campania de descuento con id '{id}' no existe.");

        discountCampaign.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ReactivateAsync(int id)
    {
        var discountCampaign = await _context.DiscountCampaigns.FirstOrDefaultAsync(campaign => campaign.Id == id)
            ?? throw new AppNotFoundException($"La campania de descuento con id '{id}' no existe.");

        discountCampaign.CancelledAt = null;
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResult<DiscountCampaignSummaryDTO>> GetAllAsync(DiscountCampaignQueryDTO query)
    {
        NormalizePagination(query);

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            throw new AppBadRequestException("El estado de la campania de descuento no es válido.");
        }

        var now = DateTime.UtcNow;

        var campaignsQuery = _context.DiscountCampaigns
            .AsNoTracking()
            .AsQueryable();

        if (query.Status.HasValue)
        {
            campaignsQuery = query.Status.Value switch
            {
                DiscountCampaignStatusOption.Scheduled => campaignsQuery.Where(campaign =>
                    !campaign.CancelledAt.HasValue && campaign.StartDate > now),
                DiscountCampaignStatusOption.Active => campaignsQuery.Where(campaign =>
                    !campaign.CancelledAt.HasValue && campaign.StartDate <= now && campaign.EndDate >= now),
                DiscountCampaignStatusOption.Finished => campaignsQuery.Where(campaign =>
                    !campaign.CancelledAt.HasValue && campaign.EndDate < now),
                DiscountCampaignStatusOption.Cancelled => campaignsQuery.Where(campaign => campaign.CancelledAt.HasValue),
                _ => campaignsQuery
            };
        }

        var totalCount = await campaignsQuery.CountAsync();
        var skip = (long)(query.Page - 1) * query.PageSize;
        List<DiscountCampaignSummaryDTO> campaigns;
        if (skip >= totalCount)
        {
            campaigns = [];
        }
        else
        {
            var campaignEntities = await campaignsQuery
                .OrderByDescending(campaign => campaign.StartDate)
                .ThenBy(campaign => campaign.Name)
                .ThenBy(campaign => campaign.Id)
                .Skip((int)skip)
                .Take(query.PageSize)
                .ToListAsync();

            campaigns = campaignEntities
                .Select(campaign => MapSummary(campaign, now))
                .ToList();
        }

        return new PaginatedResult<DiscountCampaignSummaryDTO>
        {
            Items = campaigns,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DiscountCampaignDTO> GetByIdAsync(int id)
    {
        var discountCampaign = await _context.DiscountCampaigns
            .AsNoTracking()
            .Include(campaign => campaign.DiscountCampaignProducts)
                .ThenInclude(productVariant => productVariant.Product)
            .Include(campaign => campaign.DiscountCampaignProducts)
                .ThenInclude(productVariant => productVariant.DiscountType)
            .FirstOrDefaultAsync(campaign => campaign.Id == id)
            ?? throw new AppNotFoundException($"La campania de descuento con id '{id}' no existe.");

        return MapDetail(discountCampaign, DateTime.UtcNow);
    }

    private static DiscountCampaignSummaryDTO MapSummary(DiscountCampaign campaign, DateTime now)
    {
        var status = DiscountCampaignStatusResolver.Resolve(campaign, now);

        return new DiscountCampaignSummaryDTO
        {
            Id = campaign.Id,
            Name = campaign.Name,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            StatusId = (int)status,
            StatusName = status.ToString(),
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            CreatedById = campaign.CreatedById,
            UpdatedById = campaign.UpdatedById
        };
    }

    private static DiscountCampaignDTO MapDetail(DiscountCampaign campaign, DateTime now)
    {
        var status = DiscountCampaignStatusResolver.Resolve(campaign, now);

        return new DiscountCampaignDTO
        {
            Id = campaign.Id,
            Name = campaign.Name,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            StatusId = (int)status,
            StatusName = status.ToString(),
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            CreatedById = campaign.CreatedById,
            UpdatedById = campaign.UpdatedById,
            ProductVariants = campaign.DiscountCampaignProducts
                .OrderBy(productVariant => productVariant.Product?.Name ?? string.Empty)
                .Select(productVariant => new DiscountCampaignProductDTO
                {
                    Id = productVariant.Id,
                    ProductId = productVariant.ProductId,
                    ProductName = productVariant.Product?.Name,
                    ProductCode = productVariant.Product?.Code,
                    DiscountTypeId = productVariant.DiscountTypeId,
                    DiscountTypeName = productVariant.DiscountType?.Name,
                    DiscountValue = productVariant.DiscountValue
                })
                .ToList()
        };
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

    private static void NormalizePagination(DiscountCampaignQueryDTO query)
    {
        query.Page = Math.Max(query.Page, 1);
        query.PageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
    }

    private async Task ValidateProductsAsync(IReadOnlyCollection<CreateDiscountCampaignProductDTO> productVariants)
    {
        foreach (var productVariant in productVariants)
        {
            ValidateDiscountValue(productVariant);
        }

        var repeatedProductId = productVariants
            .GroupBy(productVariant => productVariant.ProductId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();

        if (repeatedProductId > 0)
        {
            throw new AppBadRequestException($"El producto con id '{repeatedProductId}' esta repetido en la campania.");
        }

        var productIds = productVariants
            .Select(productVariant => productVariant.ProductId)
            .Distinct()
            .ToList();

        var existingProductIds = await _context.Products
            .Where(productVariant => productIds.Contains(productVariant.Id))
            .Select(productVariant => productVariant.Id)
            .ToListAsync();

        var missingProductId = productIds.Except(existingProductIds).FirstOrDefault();

        if (missingProductId > 0)
        {
            throw new AppNotFoundException($"El producto con id '{missingProductId}' no existe.");
        }

        var discountTypeIds = productVariants
            .Select(productVariant => productVariant.DiscountTypeId)
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

    private static void ValidateDiscountValue(CreateDiscountCampaignProductDTO productVariant)
    {
        if (productVariant.DiscountValue <= 0)
        {
            throw new AppBadRequestException("El valor del descuento debe ser mayor que cero.");
        }

        if (productVariant.DiscountTypeId == (int)DiscountTypeOption.Percentage && productVariant.DiscountValue > 100)
        {
            throw new AppBadRequestException("El porcentaje de descuento no puede ser mayor que 100.");
        }
    }
}
