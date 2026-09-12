using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

/// <summary>
/// Stores one R2 object that must be removed after its database owner is deleted.
/// </summary>
public class MediaCleanupItem
{
    public long Id { get; set; }
    public Guid MediaAssetId { get; set; }
    public MediaBucket Bucket { get; set; }
    public required string StorageKey { get; set; }
    public MediaCleanupStatus Status { get; set; } = MediaCleanupStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
