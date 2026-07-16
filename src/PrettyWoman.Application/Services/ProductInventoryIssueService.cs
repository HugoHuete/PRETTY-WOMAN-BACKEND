using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Extensions;
using PrettyWoman.Application.Common.Models;
using PrettyWoman.Application.DTOs.Products.InventoryIssues;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class ProductInventoryIssueService(IApplicationDbContext context) : IProductInventoryIssueService
{
    private readonly IApplicationDbContext _context = context;

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

        var product = await _context.Products
            .Include(product => product.ProductDetail)
            .FirstOrDefaultAsync(product => product.Id == createIssueDTO.ProductId)
            ?? throw new AppNotFoundException($"La variante con id '{createIssueDTO.ProductId}' no existe.");

        if (product.AvailableQuantity < createIssueDTO.Quantity)
        {
            throw new AppBadRequestException($"La variante con id '{product.Id}' no tiene suficiente inventario disponible.");
        }

        var issueDate = createIssueDTO.IssueDate.NormalizeToUtc() ?? DateTime.UtcNow;
        var issue = new ProductInventoryIssue
        {
            Product = product,
            ProductInventoryIssueTypeId = createIssueDTO.ProductInventoryIssueTypeId,
            ProductInventoryIssueStatusId = (int)ProductInventoryIssueStatusOption.Open,
            Quantity = createIssueDTO.Quantity,
            IssueDate = issueDate,
            Comments = createIssueDTO.Comments
        };

        product.AvailableQuantity -= createIssueDTO.Quantity;
        product.UnavailableQuantity += createIssueDTO.Quantity;

        issue.InventoryMovements.Add(new InventoryMovement
        {
            Product = product,
            InventoryMovementTypeId = (int)InventoryMovementTypeOption.IssueOpened,
            FromStockBucketId = (int)InventoryStockBucketOption.Available,
            ToStockBucketId = (int)InventoryStockBucketOption.Unavailable,
            Quantity = createIssueDTO.Quantity,
            MovementDate = issueDate,
            Comments = createIssueDTO.Comments
        });

        await _context.ProductInventoryIssues.AddAsync(issue);
        await _context.SaveChangesAsync();

        return issue.Id;
    }

    public async Task<ProductInventoryIssueDTO> ResolveAsync(int id, ResolveProductInventoryIssueDTO resolveIssueDTO)
    {
        NormalizeAndValidateResolution(resolveIssueDTO);
        var issue = await GetOpenIssueForUpdateAsync(id);
        var product = issue.Product!;

        if (product.UnavailableQuantity < issue.Quantity)
        {
            throw new AppBadRequestException($"La variante con id '{product.Id}' no tiene suficiente inventario no disponible.");
        }

        var status = (ProductInventoryIssueStatusOption)resolveIssueDTO.ProductInventoryIssueStatusId;
        var resolvedAt = resolveIssueDTO.ResolvedAt.NormalizeToUtc() ?? DateTime.UtcNow;
        var movementType = ResolveClosingMovementType(status);
        var toStockBucketId = ResolveClosingBucket(status);

        issue.ProductInventoryIssueStatusId = resolveIssueDTO.ProductInventoryIssueStatusId;
        issue.ResolvedAt = resolvedAt;
        issue.Comments = resolveIssueDTO.Comments ?? issue.Comments;

        product.UnavailableQuantity -= issue.Quantity;
        if (toStockBucketId == (int)InventoryStockBucketOption.Available)
        {
            product.AvailableQuantity += issue.Quantity;
        }

        issue.InventoryMovements.Add(new InventoryMovement
        {
            Product = product,
            InventoryMovementTypeId = movementType,
            FromStockBucketId = (int)InventoryStockBucketOption.Unavailable,
            ToStockBucketId = toStockBucketId,
            Quantity = issue.Quantity,
            MovementDate = resolvedAt,
            Comments = resolveIssueDTO.Comments,
        });

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
            .Include(issue => issue.Product)
                .ThenInclude(product => product!.ProductDetail)
            .Include(issue => issue.Product)
                .ThenInclude(product => product!.Size)
            .Include(issue => issue.ProductInventoryIssueType)
            .Include(issue => issue.ProductInventoryIssueStatus);
    }
    private async Task<ProductInventoryIssue> GetOpenIssueForUpdateAsync(int id)
    {
        var issue = await _context.ProductInventoryIssues
            .Include(issue => issue.Product)
            .Include(issue => issue.InventoryMovements)
            .FirstOrDefaultAsync(issue => issue.Id == id)
            ?? throw new AppNotFoundException($"El issue de inventario con id '{id}' no existe.");

        if (issue.ProductInventoryIssueStatusId != (int)ProductInventoryIssueStatusOption.Open)
        {
            throw new AppBadRequestException("Solo se pueden resolver issues abiertos.");
        }

        if (issue.Product == null)
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
        if (filters.ProductDetailId.HasValue)
        {
            query = query.Where(issue => issue.Product != null && issue.Product.ProductDetailId == filters.ProductDetailId.Value);
        }

        if (filters.ProductId.HasValue)
        {
            query = query.Where(issue => issue.ProductId == filters.ProductId.Value);
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
            ProductId = issue.ProductId,
            ProductDetailId = issue.Product != null ? issue.Product.ProductDetailId : 0,
            ProductName = issue.Product != null && issue.Product.ProductDetail != null ? issue.Product.ProductDetail.Name : null,
            ProductCode = issue.Product != null && issue.Product.ProductDetail != null ? issue.Product.ProductDetail.Code : null,
            SizeId = issue.Product != null ? issue.Product.SizeId : 0,
            SizeName = issue.Product != null && issue.Product.Size != null ? issue.Product.Size.Name : null,
            Color = issue.Product != null ? issue.Product.Color : null,
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
