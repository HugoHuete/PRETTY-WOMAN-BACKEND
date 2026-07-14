using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Interfaces;

public interface IMediaObjectStorage
{
    Task UploadAsync(MediaBucket bucket, string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(MediaBucket bucket, string storageKey, CancellationToken cancellationToken = default);
}
