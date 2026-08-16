using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.InventoryAdjustments;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class InventoryAdjustmentService(
    IApplicationDbContext context,
    IInventoryService inventoryService) : IInventoryAdjustmentService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IInventoryService _inventoryService = inventoryService;

    public async Task<PaginatedResult<InventoryAdjustmentDTO>> GetAllAsync(InventoryAdjustmentQueryDTO query)
    {
        NormalizePagination(query);

        var adjustmentsQuery = _context.InventoryAdjustments
            .AsNoTracking()
            .AsQueryable();

        adjustmentsQuery = ApplyFilters(adjustmentsQuery, query);

        var totalCount = await adjustmentsQuery.CountAsync();
        var adjustments = await IncludeAdjustmentDetails(adjustmentsQuery)
            .OrderByDescending(adjustment => adjustment.AdjustmentDate)
            .ThenByDescending(adjustment => adjustment.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PaginatedResult<InventoryAdjustmentDTO>
        {
            Items = adjustments.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<InventoryAdjustmentDTO> GetByIdAsync(int id)
    {
        var adjustment = await IncludeAdjustmentDetails(_context.InventoryAdjustments.AsNoTracking())
            .FirstOrDefaultAsync(adjustment => adjustment.Id == id)
            ?? throw new AppNotFoundException($"El ajuste de inventario con id '{id}' no existe.");

        return Map(adjustment);
    }

    public async Task<int> CreateAsync(CreateInventoryAdjustmentDTO request)
    {
        NormalizeAndValidate(request);
        await EnsureReasonExistsAsync(request.InventoryAdjustmentReasonId);

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var productVariants = await _context.ProductVariants
            .Include(productVariant => productVariant.Product)
            .Include(productVariant => productVariant.Size)
            .Where(productVariant => productIds.Contains(productVariant.Id))
            .ToListAsync();
        var missingProductId = productIds.FirstOrDefault(productId => productVariants.All(productVariant => productVariant.Id != productId));
        if (missingProductId != 0)
        {
            throw new AppNotFoundException($"La variante con id '{missingProductId}' no existe.");
        }

        var adjustmentDate = request.AdjustmentDate.NormalizeToUtc() ?? DateTime.UtcNow;
        var adjustment = new InventoryAdjustment
        {
            InventoryAdjustmentReasonId = request.InventoryAdjustmentReasonId,
            AdjustmentDate = adjustmentDate,
            Reference = request.Reference,
            Comments = request.Comments
        };

        foreach (var requestItem in request.Items)
        {
            var productVariant = productVariants.Single(productVariant => productVariant.Id == requestItem.ProductId);
            var fromStockBucket = (InventoryStockBucketOption)requestItem.FromStockBucketId;
            var toStockBucket = (InventoryStockBucketOption)requestItem.ToStockBucketId;
            EnsureManualExternalIncreaseDoesNotRepresentPurchaseSurplus(productVariant, fromStockBucket, requestItem.Quantity);
            var movement = _inventoryService.Move(
                productVariant,
                fromStockBucket,
                toStockBucket,
                requestItem.Quantity,
                InventoryMovementTypeOption.AdjustmentTransfer,
                adjustmentDate,
                requestItem.Comments ?? request.Comments);

            var item = new InventoryAdjustmentItem
            {
                ProductVariant = productVariant,
                FromStockBucketId = requestItem.FromStockBucketId,
                ToStockBucketId = requestItem.ToStockBucketId,
                Quantity = requestItem.Quantity,
                Comments = requestItem.Comments,
                InventoryMovement = movement
            };
            movement.InventoryAdjustmentItem = item;
            adjustment.Items.Add(item);
        }

        await _context.InventoryAdjustments.AddAsync(adjustment);
        await _context.SaveChangesAsync();
        return adjustment.Id;
    }

    private static void EnsureManualExternalIncreaseDoesNotRepresentPurchaseSurplus(
        ProductVariant productVariant,
        InventoryStockBucketOption fromStockBucket,
        int quantity)
    {
        if (fromStockBucket == InventoryStockBucketOption.External &&
            productVariant.ReceivedQuantity + quantity > productVariant.Quantity)
        {
            throw new AppBadRequestException(
                "Los sobrantes de compra deben registrarse desde la recepción de compras marcando la línea como sobrante.");
        }
    }

    private async Task EnsureReasonExistsAsync(int reasonId)
    {
        var exists = await _context.InventoryAdjustmentReasons
            .AsNoTracking()
            .AnyAsync(reason => reason.Id == reasonId);
        if (!exists)
        {
            throw new AppNotFoundException($"El motivo de ajuste de inventario con id '{reasonId}' no existe.");
        }
    }

    private static void NormalizeAndValidate(CreateInventoryAdjustmentDTO request)
    {
        request.Reference = request.Reference.NormalizeOptional();
        request.Comments = request.Comments.NormalizeOptional();
        request.Items ??= [];

        if (request.Items.Count == 0)
        {
            throw new AppBadRequestException("Debe enviar al menos un item de ajuste.");
        }

        if (!Enum.IsDefined(typeof(InventoryAdjustmentReasonOption), request.InventoryAdjustmentReasonId))
        {
            throw new AppBadRequestException("El motivo de ajuste de inventario no es válido.");
        }

        foreach (var item in request.Items)
        {
            item.Comments = item.Comments.NormalizeOptional();
            if (item.ProductId <= 0)
            {
                throw new AppBadRequestException("La variante del item de ajuste es obligatoria.");
            }

            if (item.Quantity <= 0)
            {
                throw new AppBadRequestException("La cantidad de cada item de ajuste debe ser mayor que cero.");
            }

            if (!Enum.IsDefined(typeof(InventoryStockBucketOption), item.FromStockBucketId) ||
                !Enum.IsDefined(typeof(InventoryStockBucketOption), item.ToStockBucketId))
            {
                throw new AppBadRequestException("Los buckets del item de ajuste no son válidos.");
            }

            if (item.FromStockBucketId == item.ToStockBucketId)
            {
                throw new AppBadRequestException("El bucket origen y destino del item de ajuste deben ser distintos.");
            }
        }

        var duplicatedItem = request.Items
            .GroupBy(item => new { item.ProductId, item.FromStockBucketId, item.ToStockBucketId })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatedItem is not null)
        {
            throw new AppBadRequestException("No puede enviar items duplicados para la misma variante y transición de inventario.");
        }
    }

    private static IQueryable<InventoryAdjustment> IncludeAdjustmentDetails(IQueryable<InventoryAdjustment> query)
        => query
            .Include(adjustment => adjustment.InventoryAdjustmentReason)
            .Include(adjustment => adjustment.Items)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(productVariant => productVariant!.Product)
            .Include(adjustment => adjustment.Items)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(productVariant => productVariant!.Size)
            .Include(adjustment => adjustment.Items)
                .ThenInclude(item => item.FromStockBucket)
            .Include(adjustment => adjustment.Items)
                .ThenInclude(item => item.ToStockBucket)
            .Include(adjustment => adjustment.Items)
                .ThenInclude(item => item.InventoryMovement);

    private static IQueryable<InventoryAdjustment> ApplyFilters(
        IQueryable<InventoryAdjustment> query,
        InventoryAdjustmentQueryDTO filters)
    {
        if (filters.InventoryAdjustmentReasonId.HasValue)
        {
            query = query.Where(adjustment => adjustment.InventoryAdjustmentReasonId == filters.InventoryAdjustmentReasonId.Value);
        }

        if (filters.ProductId.HasValue)
        {
            query = query.Where(adjustment => adjustment.Items.Any(item =>
                item.ProductVariant != null && item.ProductVariant.ProductId == filters.ProductId.Value));
        }

        if (filters.ProductVariantId.HasValue)
        {
            query = query.Where(adjustment => adjustment.Items.Any(item => item.ProductId == filters.ProductVariantId.Value));
        }

        if (filters.FromStockBucketId.HasValue)
        {
            query = query.Where(adjustment => adjustment.Items.Any(item => item.FromStockBucketId == filters.FromStockBucketId.Value));
        }

        if (filters.ToStockBucketId.HasValue)
        {
            query = query.Where(adjustment => adjustment.Items.Any(item => item.ToStockBucketId == filters.ToStockBucketId.Value));
        }

        if (filters.AdjustmentDateFrom.HasValue)
        {
            var dateFrom = filters.AdjustmentDateFrom.NormalizeToUtc()!.Value;
            query = query.Where(adjustment => adjustment.AdjustmentDate >= dateFrom);
        }

        if (filters.AdjustmentDateTo.HasValue)
        {
            var dateTo = filters.AdjustmentDateTo.NormalizeToUtc()!.Value;
            query = query.Where(adjustment => adjustment.AdjustmentDate <= dateTo);
        }

        return query;
    }

    private static InventoryAdjustmentDTO Map(InventoryAdjustment adjustment)
        => new()
        {
            Id = adjustment.Id,
            InventoryAdjustmentReasonId = adjustment.InventoryAdjustmentReasonId,
            InventoryAdjustmentReasonName = adjustment.InventoryAdjustmentReason?.Name,
            AdjustmentDate = adjustment.AdjustmentDate,
            Reference = adjustment.Reference,
            Comments = adjustment.Comments,
            CreatedAt = adjustment.CreatedAt,
            UpdatedAt = adjustment.UpdatedAt,
            Items = adjustment.Items.Select(item => new InventoryAdjustmentItemDTO
            {
                Id = item.Id,
                ProductId = item.ProductVariant?.ProductId ?? 0,
                ProductVariantId = item.ProductId,
                ProductName = item.ProductVariant?.Product?.Name,
                ProductCode = item.ProductVariant?.Product?.Code,
                SizeId = item.ProductVariant?.SizeId ?? 0,
                SizeName = item.ProductVariant?.Size?.Name,
                Variant = item.ProductVariant?.Variant,
                FromStockBucketId = item.FromStockBucketId,
                FromStockBucketName = item.FromStockBucket?.Name,
                ToStockBucketId = item.ToStockBucketId,
                ToStockBucketName = item.ToStockBucket?.Name,
                Quantity = item.Quantity,
                InventoryMovementId = item.InventoryMovement?.Id,
                Comments = item.Comments
            }).ToList()
        };

    private static void NormalizePagination(InventoryAdjustmentQueryDTO query)
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
