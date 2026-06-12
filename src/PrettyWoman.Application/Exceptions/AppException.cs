using System.Net;

namespace PrettyWoman.Application.Exceptions;

public class AppException : AppExceptionBase
{
    public AppException(string message)
        : base(message, HttpStatusCode.InternalServerError) { }

    public AppException(string message, params object[] args)
        : base(message, HttpStatusCode.InternalServerError, args) { }
}
