namespace PrettyWoman.Application.DTOs.PublicCatalog;

public sealed record CatalogQueryDTO(
    int? CategoryId = null, int? SubcategoryId = null, int? SizeId = null,
    bool OnSale = false, string Sort = "newest", int Page = 1, int PageSize = 24,
    int[]? CategoryIds = null, int[]? SubcategoryIds = null, int[]? SizeIds = null, bool MatchAnyCategory = false);

public sealed record CatalogOptionDTO(int Id, string Name);
public sealed record CatalogCategoryDTO(int Id, string Name, IReadOnlyList<CatalogOptionDTO> Subcategories);
public sealed record CatalogSizeGroupDTO(int Id, string Name, IReadOnlyList<CatalogOptionDTO> Sizes);
public sealed record CatalogFiltersDTO(
    IReadOnlyList<CatalogCategoryDTO> Categories, IReadOnlyList<CatalogOptionDTO> Sizes,
    IReadOnlyList<CatalogSizeGroupDTO> SizeGroups);
public sealed record CatalogImageDTO(string Url, int? PresentationId);
public sealed record CatalogVariantDTO(
    int Id, int SizeId, string Size, int PresentationId, string? Presentation,
    int MaxQuantity, decimal OriginalPrice, decimal FinalPrice);
public sealed record CatalogColorDTO(int Id, string? Name, string? ImageUrl);
public sealed record CatalogProductDTO(
    int Id, int Code, string Name, string? ImageUrl, IReadOnlyList<string> Sizes,
    decimal OriginalPrice, decimal FinalPrice, bool HasPriceRange, IReadOnlyList<CatalogColorDTO> Colors);
public sealed record CatalogDetailDTO(
    int Id, int Code, string Name, bool SoldOut,
    IReadOnlyList<CatalogImageDTO> Images, IReadOnlyList<CatalogVariantDTO> Variants);
public sealed record CatalogPageDTO(
    IReadOnlyList<CatalogProductDTO> Items, int TotalCount, int Page, int PageSize);
