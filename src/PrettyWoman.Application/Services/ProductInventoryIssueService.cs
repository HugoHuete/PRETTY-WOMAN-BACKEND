using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products.InventoryIssues;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class ProductInventoryIssueService(
    IApplicationDbContext context,
    IInventoryService inventoryService) : IProductInventoryIssueService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IInventoryService _inventoryService = inventoryService;

    public async Task<PaginatedResult<ProductInventoryIssueDTO>> GetAllAsync(ProductInventoryIssueQueryDTO query)
    {
        NormalizePagination(query);

        var issuesQuery = _context.ProductInventoryIssues
            .AsNoTracking()
            .AsQueryable();

        issuesQuery = ApplyFilters(issuesQuery, query);

        var totalCount = await issuesQuery.CountAsync();
        var issues = await IncludeIssueDetails(issuesQuery)
            .OrderByDescending(issue => issue.IssueDate)
            .ThenByDescending(issue => issue.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();
        var items = issues.Select(MapIssue).ToList();

        return new PaginatedResult<ProductInventoryIssueDTO>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductInventoryIssueDTO> GetByIdAsync(int id)
    {
        var issue = await IncludeIssueDetails(_context.ProductInventoryIssues.AsNoTracking())
            .FirstOrDefaultAsync(issue => issue.Id == id)
            ?? throw new AppNotFoundException($"El issue de inventario con id '{id}' no existe.");

        return MapIssue(issue);
    }

    public async Task<int> CreateAsync(CreateProductInventoryIssueDTO createIssueDTO)
    {
        NormalizeAndValidateCreate(createIssueDTO);
        await EnsureIssueTypeExistsAsync(createIssueDTO.ProductInventoryIssueTypeId);

        var productVariant = await _context.ProductVariants
            .Include(productVariant => productVariant.Product)
            .FirstOrDefaultAsync(productVariant => productVariant.Id == createIssueDTO.ProductId)
            ?? throw new AppNotFoundException($"La variante con id '{createIssueDTO.ProductId}' no existe.");

        var issueDate = createIssueDTO.IssueDate.NormalizeToUtc() ?? DateTime.UtcNow;
        var issue = new ProductInventoryIssue
        {
            ProductVariant = productVariant,
            ProductInventoryIssueTypeId = createIssueDTO.ProductInventoryIssueTypeId,
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.Open,
            Quantity = createIssueDTO.Quantity,
            IssueDate = issueDate,
            Comments = createIssueDTO.Comments
        };

        issue.InventoryMovements.Add(_inventoryService.Move(
            productVariant,
            InventoryStockBucketOption.Available,
            InventoryStockBucketOption.Unavailable,
            createIssueDTO.Quantity,
            InventoryMovementTypeOption.IssueOpened,
            issueDate,
            createIssueDTO.Comments));

        await _context.ProductInventoryIssues.AddAsync(issue);
        await _context.SaveChangesAsync();

        return issue.Id;
    }

    public async Task<ProductInventoryIssueDTO> ResolveAsync(int id, ResolveProductInventoryIssueDTO resolveIssueDTO)
    {
        NormalizeAndValidateResolution(resolveIssueDTO);
        var issue = await GetOpenIssueForUpdateAsync(id);
        var productVariant = issue.ProductVariant!;

        var status = (ProductInventoryIssueStatusOption)resolveIssueDTO.ProductInventoryIssueStatusId;
        var resolvedAt = resolveIssueDTO.ResolvedAt.NormalizeToUtc() ?? DateTime.UtcNow;
        var movementType = ResolveClosingMovementType(status);
        var toStockBucketId = ResolveClosingBucket(status);

        issue.ProductInventoryIssueStatusId = resolveIssueDTO.ProductInventoryIssueStatusId;
        issue.ResolvedAt = resolvedAt;
        issue.Comments = resolveIssueDTO.Comments ?? issue.Comments;

        issue.InventoryMovements.Add(_inventoryService.Move(
            productVariant,
            InventoryStockBucketOption.Unavailable,
            (InventoryStockBucketOption)toStockBucketId,
            issue.Quantity,
            (InventoryMovementTypeOption)movementType,
            resolvedAt,
            resolveIssueDTO.Comments));

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<ProductInventoryIssueDTO> DeleteAsync(int id)
    {
        return await ResolveAsync(id, new ResolveProductInventoryIssueDTO
        {
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.Cancelled,
            Comments = "Issue cancelado desde DELETE."
        });
    }

    private static IQueryable<ProductInventoryIssue> IncludeIssueDetails(IQueryable<ProductInventoryIssue> query)
    {
        return query
            .Include(issue => issue.ProductVariant)
                .ThenInclude(productVariant => productVariant!.Product)
            .Include(issue => issue.ProductVariant)
                .ThenInclude(productVariant => productVariant!.Size)
            .Include(issue => issue.ProductInventoryIssueType)
            .Include(issue => issue.ProductInventoryIssueStatus);
    }
    private async Task<ProductInventoryIssue> GetOpenIssueForUpdateAsync(int id)
    {
        var issue = await _context.ProductInventoryIssues
            .Include(issue => issue.ProductVariant)
            .Include(issue => issue.InventoryMovements)
            .FirstOrDefaultAsync(issue => issue.Id == id)
            ?? throw new AppNotFoundException($"El issue de inventario con id '{id}' no existe.");

        if (issue.ProductInventoryIssueStatusId != (int)ProductInventoryIssueStatusOption.Open)
        {
            throw new AppBadRequestException("Solo se pueden resolver issues abiertos.");
        }

        if (issue.ProductVariant == null)
        {
            throw new AppNotFoundException($"La variante con id '{issue.ProductId}' no existe.");
        }

        return issue;
    }

    private async Task EnsureIssueTypeExistsAsync(int issueTypeId)
    {
        var exists = await _context.ProductInventoryIssueTypes
            .AsNoTracking()
            .AnyAsync(type => type.Id == issueTypeId);

        if (!exists)
        {
            throw new AppNotFoundException($"El tipo de issue de inventario con id '{issueTypeId}' no existe.");
        }
    }

    private static IQueryable<ProductInventoryIssue> ApplyFilters(IQueryable<ProductInventoryIssue> query, ProductInventoryIssueQueryDTO filters)
    {
        if (filters.ProductId.HasValue)
        {
            query = query.Where(issue => issue.ProductVariant != null && issue.ProductVariant.ProductId == filters.ProductId.Value);
        }

        if (filters.ProductVariantId.HasValue)
        {
            query = query.Where(issue => issue.ProductId == filters.ProductVariantId.Value);
        }

        if (filters.ProductInventoryIssueTypeId.HasValue)
        {
            query = query.Where(issue => issue.ProductInventoryIssueTypeId == filters.ProductInventoryIssueTypeId.Value);
        }

        if (filters.ProductInventoryIssueStatusId.HasValue)
        {
            query = query.Where(issue => issue.ProductInventoryIssueStatusId == filters.ProductInventoryIssueStatusId.Value);
        }

        return query;
    }

    private static ProductInventoryIssueDTO MapIssue(ProductInventoryIssue issue)
    {
        return new ProductInventoryIssueDTO
        {
            Id = issue.Id,
            ProductId = issue.ProductVariant != null ? issue.ProductVariant.ProductId : 0,
            ProductVariantId = issue.ProductId,
            ProductName = issue.ProductVariant != null && issue.ProductVariant.Product != null ? issue.ProductVariant.Product.Name : null,
            ProductCode = issue.ProductVariant != null && issue.ProductVariant.Product != null ? issue.ProductVariant.Product.Code : null,
            SizeId = issue.ProductVariant != null ? issue.ProductVariant.SizeId : 0,
            SizeName = issue.ProductVariant != null && issue.ProductVariant.Size != null ? issue.ProductVariant.Size.Name : null,
            Variant = issue.ProductVariant != null ? issue.ProductVariant.Variant : null,
            ProductInventoryIssueTypeId = issue.ProductInventoryIssueTypeId,
            ProductInventoryIssueTypeName = issue.ProductInventoryIssueType != null ? issue.ProductInventoryIssueType.Name : null,
            ProductInventoryIssueStatusId = issue.ProductInventoryIssueStatusId,
            ProductInventoryIssueStatusName = issue.ProductInventoryIssueStatus != null ? issue.ProductInventoryIssueStatus.Name : null,
            Quantity = issue.Quantity,
            IssueDate = issue.IssueDate,
            ResolvedAt = issue.ResolvedAt,
            Comments = issue.Comments,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt
        };
    }

    private static void NormalizeAndValidateCreate(CreateProductInventoryIssueDTO createIssueDTO)
    {
        if (createIssueDTO.Quantity <= 0)
        {
            throw new AppBadRequestException("La cantidad debe ser mayor que cero.");
        }

        createIssueDTO.Comments = createIssueDTO.Comments.NormalizeOptional();
    }

    private static void NormalizeAndValidateResolution(ResolveProductInventoryIssueDTO resolveIssueDTO)
    {
        if (!Enum.IsDefined(typeof(ProductInventoryIssueStatusOption), resolveIssueDTO.ProductInventoryIssueStatusId))
        {
            throw new AppBadRequestException("El estado de resolución del issue no es válido.");
        }

        if (resolveIssueDTO.ProductInventoryIssueStatusId == (int)ProductInventoryIssueStatusOption.Open)
        {
            throw new AppBadRequestException("No se puede resolver un issue en estado Open.");
        }

        resolveIssueDTO.Comments = resolveIssueDTO.Comments.NormalizeOptional();
    }

    private static int ResolveClosingMovementType(ProductInventoryIssueStatusOption status)
    {
        return status switch
        {
            ProductInventoryIssueStatusOption.ResolvedToAvailable or ProductInventoryIssueStatusOption.Cancelled => (int)InventoryMovementTypeOption.IssueReturnedToAvailable,
            ProductInventoryIssueStatusOption.Discarded or ProductInventoryIssueStatusOption.ConfirmedLost => (int)InventoryMovementTypeOption.IssueRemovedFromInventory,
            _ => throw new AppBadRequestException("El estado de resolucion del issue no es valido.")
        };
    }

    private static int ResolveClosingBucket(ProductInventoryIssueStatusOption status)
    {
        return status is ProductInventoryIssueStatusOption.ResolvedToAvailable or ProductInventoryIssueStatusOption.Cancelled
            ? (int)InventoryStockBucketOption.Available
            : (int)InventoryStockBucketOption.OutOfInventory;
    }

    private static void NormalizePagination(ProductInventoryIssueQueryDTO query)
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
