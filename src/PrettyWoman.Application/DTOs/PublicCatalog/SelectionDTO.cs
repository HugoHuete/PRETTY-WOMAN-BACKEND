namespace PrettyWoman.Application.DTOs.PublicCatalog;

public sealed record SelectionItemDTO(int VariantId, int Quantity);
public sealed record CreateSelectionDTO(IReadOnlyList<SelectionItemDTO>? Items);
public sealed record SelectionLinkDTO(Guid Id, DateTime ExpiresAt);
public sealed record SelectionLineDTO(
    int VariantId, int ProductId, string Name, string Size, string? Presentation, int Quantity, decimal UnitPrice);
public sealed record SelectionLineStatusDTO(SelectionLineDTO Original, int AvailableQuantity, decimal? CurrentPrice);
public sealed record SelectionDetailDTO(Guid Id, DateTime ExpiresAt, IReadOnlyList<SelectionLineStatusDTO> Items);
public sealed record SelectionConflictDTO(int VariantId, int AvailableQuantity);
public sealed record CreateSelectionResult(
    SelectionLinkDTO? Link, IReadOnlyList<SelectionConflictDTO> Unavailable, bool KeyConflict = false);
