using System.Net;

namespace PrettyWoman.Application.Exceptions;

public class AppNotFoundException  : AppExceptionBase
{
    public AppNotFoundException (string message)
        : base(message, HttpStatusCode.NotFound) { }

    public AppNotFoundException (string message, params object[] args)
        : base(message, HttpStatusCode.NotFound, args) { }
}
