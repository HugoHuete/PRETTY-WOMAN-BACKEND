namespace PrettyWoman.Domain.Entities;

public sealed class SharedCatalogSelection
{
    public Guid Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required string Snapshot { get; set; }
}
