using System.Net;

namespace PrettyWoman.Application.Exceptions;

public class AppForbiddenException : AppExceptionBase
{
    public AppForbiddenException(string message)
        : base(message, HttpStatusCode.Forbidden) { }

    public AppForbiddenException(string message, params object[] args)
        : base(message, HttpStatusCode.Forbidden, args) { }
}