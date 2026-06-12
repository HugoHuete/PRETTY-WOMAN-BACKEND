using System.Net;

namespace PrettyWoman.Application.Exceptions;

public class AppBadRequestException : AppExceptionBase
{
    public AppBadRequestException(string message)
        : base(message, HttpStatusCode.BadRequest) { }

    public AppBadRequestException(string message, params object[] args)
        : base(message, HttpStatusCode.BadRequest, args) { }
}
