using PrettyWoman.Application.Interfaces;

namespace PrettyWoman.Application.Tests.Services.Orders;

internal sealed class TestOrderMediaUrlResolver : IMediaUrlResolver
{
    public string GetPublicUrl(string storageKey) => $"https://images.example/{storageKey}";
}
