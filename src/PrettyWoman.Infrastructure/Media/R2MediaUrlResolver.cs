using Microsoft.Extensions.Options;
using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Infrastructure.Media;

public class R2MediaUrlResolver(IOptions<R2MediaOptions> options) : IMediaUrlResolver
{
    private readonly R2MediaOptions _options = options.Value;

    public string GetPublicUrl(string storageKey)
    {
        if (!Uri.TryCreate(_options.PublicBaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Debe configurar R2Media:PublicBaseUrl con una URL HTTP(S) válida para publicar imágenes.");
        }

        return $"{baseUri.AbsoluteUri.TrimEnd('/')}/{storageKey}";
    }
}
