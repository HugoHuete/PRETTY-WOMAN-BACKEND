namespace PrettyWoman.Application.DTOs.Products;

public sealed record ProductInventorySnapshotDTO(int Code, IReadOnlyList<PresentationInventorySnapshotDTO> Presentations);

public sealed record PresentationInventorySnapshotDTO(int Id, string? Name, IReadOnlyList<SizeInventorySnapshotDTO> Sizes);

public sealed record SizeInventorySnapshotDTO(string? SizeName, int AvailableQuantity, decimal EffectiveSalePrice);
