using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.PublicCatalog;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public sealed class PublicCatalogService(IApplicationDbContext context, IMediaUrlResolver media)
{
    public async Task<CatalogPageDTO> ListAsync(CatalogQueryDTO request, CancellationToken cancellationToken = default)
    {
        var page = Math.Clamp(request.Page, 1, 100_000);
        var pageSize = Math.Clamp(request.PageSize, 1, 60);
        var categoryIds = FilterIds(request.CategoryIds, request.CategoryId);
        var subcategoryIds = FilterIds(request.SubcategoryIds, request.SubcategoryId);
        var sizeIds = FilterIds(request.SizeIds, request.SizeId);
        var candidates = context.Products.AsNoTracking()
            .Where(product => product.IsPublishedInCatalog && product.ProductVariants.Any(variant =>
                variant.AvailableQuantity > 0 && (sizeIds.Length == 0 || sizeIds.Contains(variant.SizeId))));
        if (request.MatchAnyCategory && categoryIds.Length > 0 && subcategoryIds.Length > 0)
        {
            candidates = candidates.Where(product => subcategoryIds.Contains(product.SubcategoryId) ||
                (product.Subcategory != null && categoryIds.Contains(product.Subcategory.CategoryId)));
        }
        else
        {
            if (categoryIds.Length > 0)
                candidates = candidates.Where(product => product.Subcategory != null && categoryIds.Contains(product.Subcategory.CategoryId));
            if (subcategoryIds.Length > 0)
                candidates = candidates.Where(product => subcategoryIds.Contains(product.SubcategoryId));
        }

        var now = DateTime.UtcNow;
        if (!request.OnSale && request.Sort is not ("priceAsc" or "priceDesc"))
        {
            var count = await candidates.CountAsync(cancellationToken);
            var newest = await WithImages(WithVariants(candidates.OrderByDescending(product => product.Code)
                .ThenByDescending(product => product.Id).Skip((page - 1) * pageSize).Take(pageSize)))
                .ToListAsync(cancellationToken);
            return new(newest.Select(product => (Product: product, Variants: MatchingVariants(product, sizeIds, request.OnSale, now)))
                .Where(row => row.Variants.Length > 0)
                .Select(row => Summarize(row.Product, row.Variants, ReadImages(row.Product)))
                .ToArray(), count, page, pageSize);
        }

        // Filtrar y ordenar en SQL antes de cargar las colecciones de la página.
        var pricedVariants =
            from variant in context.ProductVariants.AsNoTracking()
            join product in candidates on variant.ProductId equals product.Id
            where variant.AvailableQuantity > 0 && (sizeIds.Length == 0 || sizeIds.Contains(variant.SizeId))
            let discount = (from target in context.DiscountCampaignProducts
                where (target.ProductPresentationId == variant.ProductPresentationId || target.ProductId == variant.ProductId) &&
                      target.DiscountCampaign != null && target.DiscountCampaign.CancelledAt == null &&
                      target.DiscountCampaign.StartDate <= now && target.DiscountCampaign.EndDate >= now
                let raw = target.DiscountTypeId == (int)DiscountTypeOption.FixedAmount ? variant.SalePrice - target.DiscountValue :
                          target.DiscountTypeId == (int)DiscountTypeOption.Percentage ? variant.SalePrice * (1 - target.DiscountValue / 100) :
                          target.DiscountTypeId == (int)DiscountTypeOption.FixedPrice ? target.DiscountValue : variant.SalePrice
                let price = Math.Floor(Math.Max(0m, raw) * 100m + 0.5m) / 100m
                where price < variant.SalePrice
                orderby target.ProductPresentationId != null descending, price, target.DiscountCampaignId
                select (decimal?)price).FirstOrDefault()
            select new { product.Id, product.Code, OriginalPrice = variant.SalePrice, FinalPrice = discount ?? variant.SalePrice };

        if (request.OnSale)
            pricedVariants = pricedVariants.Where(variant => variant.FinalPrice < variant.OriginalPrice);

        var grouped = pricedVariants.GroupBy(variant => new { variant.Id, variant.Code })
            .Select(group => new { group.Key.Id, group.Key.Code, FinalPrice = group.Min(variant => variant.FinalPrice) });
        var countFiltered = await grouped.CountAsync(cancellationToken);
        var ordered = request.Sort switch
        {
            "priceAsc" => grouped.OrderBy(product => product.FinalPrice).ThenByDescending(product => product.Code),
            "priceDesc" => grouped.OrderByDescending(product => product.FinalPrice).ThenByDescending(product => product.Code),
            _ => grouped.OrderByDescending(product => product.Code)
        };
        var pageIds = await ordered.ThenByDescending(product => product.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(product => product.Id).ToArrayAsync(cancellationToken);
        var pageProducts = await WithImages(WithVariants(context.Products.AsNoTracking().Where(product => pageIds.Contains(product.Id))))
            .ToListAsync(cancellationToken);
        var productsById = pageProducts.ToDictionary(product => product.Id);
        // El inventario o los descuentos pueden cambiar después de seleccionar la página.
        var items = new List<CatalogProductDTO>(pageIds.Length);
        foreach (var id in pageIds)
        {
            if (!productsById.TryGetValue(id, out var product))
                continue;

            var variants = MatchingVariants(product, sizeIds, request.OnSale, now);
            if (variants.Length > 0)
                items.Add(Summarize(product, variants, ReadImages(product)));
        }
        return new(items, countFiltered, page, pageSize);
    }

    public async Task<CatalogDetailDTO?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await WithImages(WithVariants(context.Products.AsNoTracking().Where(product => product.Id == id && product.IsPublishedInCatalog &&
            product.ProductVariants.Any(variant => variant.ReceivedQuantity > 0 || variant.AvailableQuantity > 0))))
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null)
            return null;

        var variants = ReadVariants(product, DateTime.UtcNow).ToArray();
        return new(product.Id, product.Code, product.Name, variants.All(variant => variant.MaxQuantity == 0),
            ReadImages(product), variants);
    }

    public async Task<CatalogFiltersDTO> FiltersAsync(CancellationToken cancellationToken = default)
    {
        var stockedSubcategories = await context.Subcategories.AsNoTracking()
            .Where(subcategory => subcategory.Products.Any(product => product.IsPublishedInCatalog &&
                product.ProductVariants.Any(variant => variant.AvailableQuantity > 0)))
            .OrderBy(subcategory => subcategory.Category!.Name).ThenBy(subcategory => subcategory.Name)
            .Select(subcategory => new
            {
                subcategory.CategoryId,
                CategoryName = subcategory.Category!.Name,
                subcategory.Id,
                subcategory.Name
            })
            .ToListAsync(cancellationToken);
        var categories = stockedSubcategories.GroupBy(subcategory => new { subcategory.CategoryId, subcategory.CategoryName })
            .Select(group => new CatalogCategoryDTO(group.Key.CategoryId, group.Key.CategoryName,
                group.Select(subcategory => new CatalogOptionDTO(subcategory.Id, subcategory.Name)).ToArray()))
            .ToArray();
        var sizes = await context.Sizes.AsNoTracking()
            .Where(size => context.ProductVariants.Any(variant => variant.SizeId == size.Id &&
                variant.AvailableQuantity > 0 && variant.Product != null && variant.Product.IsPublishedInCatalog))
            .OrderBy(size => size.SizeGroupId).ThenBy(size => size.DisplayOrder)
            .ThenBy(size => size.Name).ThenBy(size => size.Id)
            .Select(size => new
            {
                size.SizeGroupId,
                GroupName = size.SizeGroup!.Name,
                Option = new CatalogOptionDTO(size.Id, size.Name)
            }).ToListAsync(cancellationToken);
        var sizeGroups = sizes.GroupBy(size => new { size.SizeGroupId, size.GroupName })
            .Select(group => new CatalogSizeGroupDTO(group.Key.SizeGroupId, group.Key.GroupName,
                group.Select(size => size.Option).ToArray()))
            .ToArray();
        return new(categories, sizes.Select(size => size.Option).ToArray(), sizeGroups);
    }

    internal static CatalogVariantDTO PriceVariant(Product product, ProductPresentation presentation,
        ProductVariant variant, DateTime now)
    {
        var discount = ProductService.GetBestActiveDiscount(product, presentation, variant, now);
        return new(variant.Id, variant.SizeId, variant.Size?.Name ?? "", presentation.Id, presentation.Name,
            Math.Max(0, variant.AvailableQuantity), variant.SalePrice, discount?.DiscountedSalePrice ?? variant.SalePrice);
    }

    private static IQueryable<Product> WithVariants(IQueryable<Product> products) => products
        .Include(product => product.ProductPresentations).ThenInclude(presentation => presentation.Color)
        .Include(product => product.DiscountCampaignProducts).ThenInclude(target => target.DiscountCampaign)
        .Include(product => product.ProductPresentations).ThenInclude(presentation => presentation.DiscountCampaignProducts)
            .ThenInclude(target => target.DiscountCampaign)
        .Include(product => product.ProductPresentations).ThenInclude(presentation => presentation.ProductVariants)
            .ThenInclude(variant => variant.Size)
        .AsSplitQuery();

    private static IQueryable<Product> WithImages(IQueryable<Product> products) => products
        .Include(product => product.ProductImages).ThenInclude(image => image.MediaAsset)
            .ThenInclude(asset => asset!.Variants).AsSplitQuery();

    private static IEnumerable<CatalogVariantDTO> ReadVariants(Product product, DateTime now) =>
        product.ProductPresentations.OrderBy(presentation => presentation.SortOrder).ThenBy(presentation => presentation.Id)
            .SelectMany(presentation => presentation.ProductVariants.OrderBy(variant => variant.Size?.DisplayOrder)
                .ThenBy(variant => variant.Id).Select(variant => PriceVariant(product, presentation, variant, now)));

    private static int[] FilterIds(int[]? multiple, int? single) =>
        multiple is { Length: > 0 } ? multiple : single is int id ? [id] : [];

    private static CatalogVariantDTO[] MatchingVariants(Product product, int[] sizeIds, bool onSale, DateTime now) =>
        ReadVariants(product, now).Where(variant => variant.MaxQuantity > 0 &&
            (sizeIds.Length == 0 || sizeIds.Contains(variant.SizeId)) &&
            (!onSale || variant.FinalPrice < variant.OriginalPrice)).ToArray();

    private CatalogImageDTO[] ReadImages(Product product) => product.ProductImages
        .Where(image => image.MediaAsset is { Visibility: MediaVisibility.Public, Status: MediaAssetStatus.Ready })
        .OrderByDescending(image => image.IsPrimary).ThenBy(image => image.SortOrder).ThenBy(image => image.Id)
        .Select(image => (image.ProductPresentationId, Key: image.MediaAsset?.Variants
            .FirstOrDefault(variant => variant.Type == MediaVariantType.Web && variant.Bucket == MediaBucket.Public)?.StorageKey))
        .Where(image => image.Key is not null)
        .Select(image => new CatalogImageDTO(media.GetPublicUrl(image.Key!), image.ProductPresentationId)).ToArray();

    private static CatalogProductDTO Summarize(Product product, CatalogVariantDTO[] variants, CatalogImageDTO[] images)
    {
        var cheapest = variants.MinBy(variant => variant.FinalPrice)!;
        var presentations = product.ProductPresentations.ToDictionary(presentation => presentation.Id);
        var colors = variants.DistinctBy(variant => variant.PresentationId)
            .Select(variant =>
            {
                var color = presentations.GetValueOrDefault(variant.PresentationId)?.Color;
                return new CatalogColorDTO(variant.PresentationId, variant.Presentation ?? color?.Name,
                    images.FirstOrDefault(image => image.PresentationId == variant.PresentationId)?.Url,
                    color?.HexCode);
            })
            .ToArray();
        var cover = images.FirstOrDefault(image => colors.Any(color => color.Id == image.PresentationId))
            ?? images.FirstOrDefault(image => image.PresentationId is null);
        var matchingSizeIds = variants.Select(variant => variant.SizeId).ToHashSet();
        var sizes = product.ProductPresentations.SelectMany(presentation => presentation.ProductVariants)
            .Where(variant => matchingSizeIds.Contains(variant.SizeId))
            .Select(variant => variant.Size)
            .OfType<Size>()
            .DistinctBy(size => size.Id)
            .OrderBy(size => size.SizeGroupId).ThenBy(size => size.DisplayOrder)
            .ThenBy(size => size.Name).ThenBy(size => size.Id)
            .Select(size => size.Name).Distinct().ToArray();
        return new(product.Id, product.Code, product.Name, cover?.Url,
            sizes, cheapest.OriginalPrice, cheapest.FinalPrice,
            variants.Any(variant => variant.FinalPrice != cheapest.FinalPrice), colors);
    }
}
