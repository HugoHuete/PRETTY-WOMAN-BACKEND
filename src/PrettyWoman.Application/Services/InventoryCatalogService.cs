using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.InventoryCatalogs;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Application.Services;

public class InventoryCatalogService(IApplicationDbContext context) : IInventoryCatalogService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<IEnumerable<InventoryCatalogItemDTO>> GetAdjustmentReasonsAsync()
    {
        return await _context.InventoryAdjustmentReasons
            .AsNoTracking()
            .OrderBy(reason => reason.Id)
            .Select(reason => new InventoryCatalogItemDTO
            {
                Id = reason.Id,
                Name = reason.Name
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryCatalogItemDTO>> GetStockBucketsAsync()
    {
        return await _context.InventoryStockBuckets
            .AsNoTracking()
            .OrderBy(bucket => bucket.Id)
            .Select(bucket => new InventoryCatalogItemDTO
            {
                Id = bucket.Id,
                Name = bucket.Name
            })
            .ToListAsync();
    }
}
