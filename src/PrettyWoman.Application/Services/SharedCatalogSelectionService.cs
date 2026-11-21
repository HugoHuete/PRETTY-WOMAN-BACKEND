using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.PublicCatalog;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Services;

public sealed class SharedCatalogSelectionService(IApplicationDbContext context)
{
    public const int MaximumLines = 30;

    public async Task<CreateSelectionResult> CreateAsync(Guid key, IReadOnlyList<SelectionItemDTO> items,
        CancellationToken cancellationToken = default)
    {
        var ordered = items.OrderBy(item => item.VariantId).ToArray();
        var existing = await FindByKeyAsync(key, cancellationToken);
        if (existing is not null)
            return CheckExisting(existing, ordered);

        var now = DateTime.UtcNow;
        var current = await ReadVariantsAsync(ordered.Select(item => item.VariantId), now, cancellationToken,
            publishedOnly: true);
        var conflicts = ordered.Where(item => (current.GetValueOrDefault(item.VariantId)?.Variant.MaxQuantity ?? 0) < item.Quantity)
            .Select(item => new SelectionConflictDTO(item.VariantId, current.GetValueOrDefault(item.VariantId)?.Variant.MaxQuantity ?? 0))
            .ToArray();
        if (conflicts.Length > 0)
            return new(null, conflicts);

        var lines = ordered.Select(item =>
        {
            var row = current[item.VariantId];
            return new SelectionLineDTO(item.VariantId, row.ProductId, row.Name, row.Variant.Size,
                row.Variant.Presentation, item.Quantity, row.Variant.FinalPrice);
        }).ToArray();
        // PostgreSQL stores timestamps at microsecond precision.
        var expiresAt = now.AddDays(30);
        expiresAt = expiresAt.AddTicks(-(expiresAt.Ticks % TimeSpan.TicksPerMicrosecond));
        var selection = new SharedCatalogSelection
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = key,
            ExpiresAt = expiresAt,
            Snapshot = JsonSerializer.Serialize(lines)
        };
        context.SharedCatalogSelections.Add(selection);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            existing = await FindByKeyAsync(key, cancellationToken);
            if (existing is null)
                throw;
            return CheckExisting(existing, ordered);
        }
        return new(new(selection.Id, selection.ExpiresAt), []);
    }

    public async Task<(SelectionDetailDTO? Detail, bool Expired)> GetAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        var selection = await context.SharedCatalogSelections.AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (selection is null)
            return (null, false);
        var now = DateTime.UtcNow;
        if (selection.ExpiresAt <= now)
            return (null, true);

        var originals = ReadSnapshot(selection);
        var current = await ReadVariantsAsync(originals.Select(item => item.VariantId), now, cancellationToken);
        var lines = originals.Select(item =>
        {
            var live = current.GetValueOrDefault(item.VariantId)?.Variant;
            return new SelectionLineStatusDTO(item, live?.MaxQuantity ?? 0, live?.FinalPrice);
        }).ToArray();
        return (new(selection.Id, selection.ExpiresAt, lines), false);
    }

    private Task<SharedCatalogSelection?> FindByKeyAsync(Guid key, CancellationToken cancellationToken) =>
        context.SharedCatalogSelections.AsNoTracking()
            .SingleOrDefaultAsync(row => row.IdempotencyKey == key, cancellationToken);

    private static SelectionLineDTO[] ReadSnapshot(SharedCatalogSelection selection) =>
        JsonSerializer.Deserialize<SelectionLineDTO[]>(selection.Snapshot)!;

    private static CreateSelectionResult CheckExisting(SharedCatalogSelection selection, SelectionItemDTO[] ordered)
    {
        var matches = ReadSnapshot(selection).Select(line => new SelectionItemDTO(line.VariantId, line.Quantity))
            .SequenceEqual(ordered);
        return matches && selection.ExpiresAt > DateTime.UtcNow
            ? new(new(selection.Id, selection.ExpiresAt), [])
            : new(null, [], true);
    }

    private async Task<Dictionary<int, LiveVariant>> ReadVariantsAsync(IEnumerable<int> variantIds, DateTime now,
        CancellationToken cancellationToken, bool publishedOnly = false)
    {
        var ids = variantIds.ToArray();
        var variants = await context.ProductVariants.AsNoTracking()
            .Where(variant => ids.Contains(variant.Id) && (!publishedOnly || variant.Product!.IsPublishedInCatalog))
            .Include(variant => variant.Size)
            .Include(variant => variant.Product).ThenInclude(product => product!.DiscountCampaignProducts)
                .ThenInclude(target => target.DiscountCampaign)
            .Include(variant => variant.ProductPresentation).ThenInclude(presentation => presentation!.DiscountCampaignProducts)
                .ThenInclude(target => target.DiscountCampaign)
            .AsSplitQuery().ToListAsync(cancellationToken);
        return variants.ToDictionary(variant => variant.Id, variant => new LiveVariant(variant.ProductId,
            variant.Product!.Name, PublicCatalogService.PriceVariant(variant.Product, variant.ProductPresentation!, variant, now)));
    }

    private sealed record LiveVariant(int ProductId, string Name, CatalogVariantDTO Variant);
}
