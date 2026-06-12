using System.Net;

namespace PrettyWoman.Application.Exceptions;

public class AppUnauthorizedException : AppExceptionBase
{
    public AppUnauthorizedException(string message)
        : base(message, HttpStatusCode.Unauthorized) { }

    public AppUnauthorizedException(string message, params object[] args)
        : base(message, HttpStatusCode.Unauthorized, args) { }
}