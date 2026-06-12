using System.Net;

namespace PrettyWoman.Application.Exceptions;

public class AppUnsupportedMediaTypeException : AppExceptionBase
{
    public AppUnsupportedMediaTypeException(string message)
        : base(message, HttpStatusCode.UnsupportedMediaType) { }

    public AppUnsupportedMediaTypeException(string message, params object[] args)
        : base(message, HttpStatusCode.UnsupportedMediaType, args) { }
}
