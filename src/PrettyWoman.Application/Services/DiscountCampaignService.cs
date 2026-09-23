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
    private const decimal MaxDiscountValue = 9_999_999_999m;

    public async Task<int> CreateAsync(CreateDiscountCampaignDTO createDiscountCampaignDTO)
    {
        createDiscountCampaignDTO.Name = NormalizeAndValidateCampaign(
            createDiscountCampaignDTO.Name,
            createDiscountCampaignDTO.StartDate,
            createDiscountCampaignDTO.EndDate);
        await ValidateTargetsAsync(createDiscountCampaignDTO.Targets);
        await EnsureNameIsUniqueAsync(createDiscountCampaignDTO.Name);

        var discountCampaign = new DiscountCampaign
        {
            Name = createDiscountCampaignDTO.Name,
            StartDate = createDiscountCampaignDTO.StartDate,
            EndDate = createDiscountCampaignDTO.EndDate,
            CancelledAt = null,
            DiscountCampaignProducts = createDiscountCampaignDTO.Targets
                .Select(target => new DiscountCampaignProduct
                {
                    ProductId = target.ProductId,
                    ProductPresentationId = target.ProductPresentationId,
                    DiscountTypeId = target.DiscountTypeId,
                    DiscountValue = target.DiscountValue
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

        updateDiscountCampaignDTO.Name = NormalizeAndValidateCampaign(
            updateDiscountCampaignDTO.Name,
            updateDiscountCampaignDTO.StartDate,
            updateDiscountCampaignDTO.EndDate);

        await ValidateTargetsAsync(updateDiscountCampaignDTO.Targets);
        await EnsureNameIsUniqueAsync(updateDiscountCampaignDTO.Name, id);

        discountCampaign.Name = updateDiscountCampaignDTO.Name;
        discountCampaign.StartDate = updateDiscountCampaignDTO.StartDate;
        discountCampaign.EndDate = updateDiscountCampaignDTO.EndDate;

        var existingTargetsByKey = discountCampaign.DiscountCampaignProducts
            .ToDictionary(GetTargetKey);
        var requestedTargets = updateDiscountCampaignDTO.Targets
            .Select(GetTargetKey)
            .ToHashSet();

        var targetsToRemove = discountCampaign.DiscountCampaignProducts
            .Where(existingTarget => !requestedTargets.Contains(GetTargetKey(existingTarget)))
            .ToList();

        _context.DiscountCampaignProducts.RemoveRange(targetsToRemove);

        foreach (var target in updateDiscountCampaignDTO.Targets)
        {
            if (existingTargetsByKey.TryGetValue(GetTargetKey(target), out var existingTarget))
            {
                existingTarget.DiscountTypeId = target.DiscountTypeId;
                existingTarget.DiscountValue = target.DiscountValue;
                continue;
            }

            discountCampaign.DiscountCampaignProducts.Add(new DiscountCampaignProduct
            {
                ProductId = target.ProductId,
                ProductPresentationId = target.ProductPresentationId,
                DiscountTypeId = target.DiscountTypeId,
                DiscountValue = target.DiscountValue
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

    public async Task<DiscountCampaignImportResultDTO> ValidateImportAsync(
        int campaignId,
        DiscountCampaignImportRequestDTO request)
    {
        await EnsureCampaignExistsAsync(campaignId);

        var resolvedRows = await ResolveImportRowsAsync(request.Rows ?? []);

        return new DiscountCampaignImportResultDTO
        {
            IsValid = resolvedRows.Count > 0 && resolvedRows.All(row => row.Errors.Count == 0),
            Rows = resolvedRows.Select(MapImportRow).ToList()
        };
    }

    public async Task ImportAsync(int campaignId, DiscountCampaignImportRequestDTO request)
    {
        var campaign = await _context.DiscountCampaigns
            .Include(discountCampaign => discountCampaign.DiscountCampaignProducts)
            .FirstOrDefaultAsync(discountCampaign => discountCampaign.Id == campaignId)
            ?? throw new AppNotFoundException($"La campania de descuento con id '{campaignId}' no existe.");

        var resolvedRows = await ResolveImportRowsAsync(request.Rows ?? []);
        var invalidRows = resolvedRows.Where(row => row.Errors.Count > 0).ToList();

        if (resolvedRows.Count == 0 || invalidRows.Count > 0)
        {
            var errors = invalidRows
                .SelectMany(row => row.Errors.Select(error => $"Fila {row.Input.RowNumber}: {error}"))
                .ToList();

            if (errors.Count == 0)
            {
                errors.Add("La importacion debe contener al menos una fila valida.");
            }

            throw new AppBadRequestException(string.Join(" ", errors));
        }

        _context.DiscountCampaignProducts.RemoveRange(campaign.DiscountCampaignProducts);
        campaign.DiscountCampaignProducts = resolvedRows
            .Select(row => new DiscountCampaignProduct
            {
                ProductId = row.Input.ProductPresentationId.HasValue ? null : row.ProductId,
                ProductPresentationId = row.Input.ProductPresentationId,
                DiscountTypeId = row.Input.DiscountTypeId,
                DiscountValue = row.Input.DiscountValue
            })
            .ToList();

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
                .ThenInclude(target => target.Product)
            .Include(campaign => campaign.DiscountCampaignProducts)
                .ThenInclude(target => target.ProductPresentation)
                    .ThenInclude(presentation => presentation!.Product)
            .Include(campaign => campaign.DiscountCampaignProducts)
                .ThenInclude(target => target.DiscountType)
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
            Targets = campaign.DiscountCampaignProducts
                .OrderBy(target => target.Product?.Name ?? target.ProductPresentation?.Product?.Name ?? string.Empty)
                .ThenBy(target => target.ProductPresentation?.Name)
                .Select(target => new DiscountCampaignTargetDTO
                {
                    Id = target.Id,
                    ProductId = target.ProductId ?? target.ProductPresentation?.ProductId,
                    ProductPresentationId = target.ProductPresentationId,
                    ProductName = target.Product?.Name ?? target.ProductPresentation?.Product?.Name,
                    ProductCode = target.Product?.Code ?? target.ProductPresentation?.Product?.Code,
                    PresentationName = target.ProductPresentation?.Name,
                    DiscountTypeId = target.DiscountTypeId,
                    DiscountTypeName = target.DiscountType?.Name,
                    DiscountValue = target.DiscountValue
                })
                .ToList()
        };
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

    private async Task ValidateTargetsAsync(IReadOnlyCollection<CreateDiscountCampaignTargetDTO> targets)
    {
        foreach (var target in targets)
        {
            ValidateDiscountValue(target);
        }

        var invalidTarget = targets
            .FirstOrDefault(target => target.ProductId.HasValue == target.ProductPresentationId.HasValue);

        if (invalidTarget is not null)
        {
            throw new AppBadRequestException("Cada regla de descuento debe tener exactamente uno de estos destinos: producto o presentacion.");
        }

        var repeatedProductId = targets
            .Where(target => target.ProductId.HasValue)
            .GroupBy(target => target.ProductId!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();

        if (repeatedProductId > 0)
        {
            throw new AppBadRequestException($"El producto con id '{repeatedProductId}' esta repetido en la campania.");
        }

        var repeatedPresentationId = targets
            .Where(target => target.ProductPresentationId.HasValue)
            .GroupBy(target => target.ProductPresentationId!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();

        if (repeatedPresentationId > 0)
        {
            throw new AppBadRequestException($"La presentacion con id '{repeatedPresentationId}' esta repetida en la campania.");
        }

        var productIds = targets
            .Where(target => target.ProductId.HasValue)
            .Select(target => target.ProductId!.Value)
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

        var presentationIds = targets
            .Where(target => target.ProductPresentationId.HasValue)
            .Select(target => target.ProductPresentationId!.Value)
            .Distinct()
            .ToList();

        var existingPresentationIds = await _context.ProductPresentations
            .Where(presentation => presentationIds.Contains(presentation.Id))
            .Select(presentation => presentation.Id)
            .ToListAsync();

        var missingPresentationId = presentationIds.Except(existingPresentationIds).FirstOrDefault();

        if (missingPresentationId > 0)
        {
            throw new AppNotFoundException($"La presentacion con id '{missingPresentationId}' no existe.");
        }

        var discountTypeIds = targets
            .Select(target => target.DiscountTypeId)
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

    private static string GetTargetKey(CreateDiscountCampaignTargetDTO target) =>
        target.ProductId.HasValue
            ? $"product:{target.ProductId}"
            : $"presentation:{target.ProductPresentationId}";

    private static string GetTargetKey(DiscountCampaignProduct target) =>
        target.ProductId.HasValue
            ? $"product:{target.ProductId}"
            : $"presentation:{target.ProductPresentationId}";

    private async Task EnsureCampaignExistsAsync(int campaignId)
    {
        var exists = await _context.DiscountCampaigns
            .AnyAsync(campaign => campaign.Id == campaignId);

        if (!exists)
        {
            throw new AppNotFoundException($"La campania de descuento con id '{campaignId}' no existe.");
        }
    }

    private async Task<List<ResolvedImportRow>> ResolveImportRowsAsync(
        IReadOnlyCollection<DiscountCampaignImportRowDTO> rows)
    {
        if (rows.Any(row => row is null))
        {
            throw new AppBadRequestException("La importacion no puede contener filas nulas.");
        }

        var resolvedRows = rows
            .Select(row => new ResolvedImportRow(row))
            .ToList();

        var productCodes = rows
            .Select(row => row.ProductCode)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .Where(product => productCodes.Contains(product.Code))
            .ToDictionaryAsync(product => product.Code);

        var presentationIds = rows
            .Where(row => row.ProductPresentationId.HasValue)
            .Select(row => row.ProductPresentationId!.Value)
            .Distinct()
            .ToList();

        var presentations = await _context.ProductPresentations
            .Where(presentation => presentationIds.Contains(presentation.Id))
            .Select(presentation => new ImportPresentationInfo(
                presentation.Id,
                presentation.ProductId,
                presentation.Product!.Code,
                presentation.Product.Name,
                presentation.Name))
            .ToDictionaryAsync(presentation => presentation.Id);

        var discountTypeIds = rows
            .Select(row => row.DiscountTypeId)
            .Distinct()
            .ToList();

        var existingDiscountTypeIds = (await _context.DiscountTypes
            .Where(discountType => discountTypeIds.Contains(discountType.Id))
            .Select(discountType => discountType.Id)
            .ToListAsync())
            .ToHashSet();

        foreach (var resolvedRow in resolvedRows)
        {
            var input = resolvedRow.Input;

            if (input.RowNumber <= 0)
            {
                resolvedRow.Errors.Add("El numero de fila debe ser mayor que cero.");
            }

            if (!products.TryGetValue(input.ProductCode, out var product))
            {
                resolvedRow.Errors.Add($"El codigo de producto '{input.ProductCode}' no existe.");
            }
            else
            {
                resolvedRow.Product = product;
                resolvedRow.ProductId = product.Id;
            }

            if (input.ProductPresentationId.HasValue)
            {
                if (!presentations.TryGetValue(input.ProductPresentationId.Value, out var presentation))
                {
                    resolvedRow.Errors.Add(
                        $"La presentacion con id '{input.ProductPresentationId.Value}' no existe.");
                }
                else
                {
                    resolvedRow.Presentation = presentation;

                    if (resolvedRow.Product is not null &&
                        presentation.ProductId != resolvedRow.Product.Id)
                    {
                        resolvedRow.Errors.Add(
                            $"La presentacion con id '{presentation.Id}' no pertenece al producto con codigo '{input.ProductCode}'.");
                    }
                }
            }

            if (!existingDiscountTypeIds.Contains(input.DiscountTypeId))
            {
                resolvedRow.Errors.Add($"El tipo de descuento con id '{input.DiscountTypeId}' no existe.");
            }

            if (input.DiscountValue <= 0)
            {
                resolvedRow.Errors.Add("El valor del descuento debe ser mayor que cero.");
            }
            else if (input.DiscountValue > MaxDiscountValue)
            {
                resolvedRow.Errors.Add("El valor del descuento no puede ser mayor que 9999999999.");
            }
            else if (input.DiscountTypeId == (int)DiscountTypeOption.Percentage &&
                     input.DiscountValue > 100)
            {
                resolvedRow.Errors.Add("El porcentaje de descuento no puede ser mayor que 100.");
            }

            if (resolvedRow.Errors.Count == 0)
            {
                resolvedRow.TargetKey = input.ProductPresentationId.HasValue
                    ? $"presentation:{input.ProductPresentationId}"
                    : $"product:{resolvedRow.ProductId}";
            }
        }

        var repeatedTargets = resolvedRows
            .Where(row => row.TargetKey is not null)
            .GroupBy(row => row.TargetKey!)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .ToList();

        foreach (var repeatedTarget in repeatedTargets)
        {
            repeatedTarget.Errors.Add("El destino esta repetido en la importacion.");
        }

        return resolvedRows;
    }

    private static DiscountCampaignImportRowResultDTO MapImportRow(ResolvedImportRow row)
    {
        return new DiscountCampaignImportRowResultDTO
        {
            RowNumber = row.Input.RowNumber,
            ProductCode = row.Input.ProductCode,
            ProductId = row.ProductId,
            ProductPresentationId = row.Input.ProductPresentationId,
            ProductName = row.Product?.Name ?? row.Presentation?.ProductName,
            PresentationName = row.Presentation?.Name,
            DiscountTypeId = row.Input.DiscountTypeId,
            DiscountValue = row.Input.DiscountValue,
            IsValid = row.Errors.Count == 0,
            Errors = row.Errors
        };
    }

    private sealed class ResolvedImportRow(DiscountCampaignImportRowDTO input)
    {
        public DiscountCampaignImportRowDTO Input { get; } = input;
        public Product? Product { get; set; }
        public ImportPresentationInfo? Presentation { get; set; }
        public int? ProductId { get; set; }
        public string? TargetKey { get; set; }
        public List<string> Errors { get; } = [];
    }

    private sealed record ImportPresentationInfo(
        int Id,
        int ProductId,
        int ProductCode,
        string ProductName,
        string? Name);

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

    private static void ValidateDiscountValue(CreateDiscountCampaignTargetDTO target)
    {
        if (target.DiscountValue <= 0)
        {
            throw new AppBadRequestException("El valor del descuento debe ser mayor que cero.");
        }

        if (target.DiscountValue > MaxDiscountValue)
        {
            throw new AppBadRequestException("El valor del descuento no puede ser mayor que 9999999999.");
        }

        if (target.DiscountTypeId == (int)DiscountTypeOption.Percentage && target.DiscountValue > 100)
        {
            throw new AppBadRequestException("El porcentaje de descuento no puede ser mayor que 100.");
        }
    }
}
