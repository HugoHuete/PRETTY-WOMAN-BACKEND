using System.Net;

namespace PrettyWoman.Application.Exceptions;

public class AppCustomException : AppExceptionBase
{
    public AppCustomException(string message, HttpStatusCode statusCode)
        : base(message, statusCode) { }

    public AppCustomException(string message, HttpStatusCode statusCode, params object[] args)
        : base(message, statusCode, args) { }
}
