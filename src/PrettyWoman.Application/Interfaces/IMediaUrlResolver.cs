namespace PrettyWoman.Application.Interfaces;

public interface IMediaUrlResolver
{
    string GetPublicUrl(string storageKey);
}
