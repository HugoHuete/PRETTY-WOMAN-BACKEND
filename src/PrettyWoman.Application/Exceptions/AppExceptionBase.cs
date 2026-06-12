using System.Globalization;
using System.Net;

namespace PrettyWoman.Application.Exceptions;

public abstract class AppExceptionBase : Exception
{
    public HttpStatusCode StatusCode { get; }

    protected AppExceptionBase(string message, HttpStatusCode statusCode)
        : base(message) =>
        StatusCode = statusCode;

    protected AppExceptionBase(string message, HttpStatusCode statusCode, params object[] args)
        : base(string.Format(CultureInfo.CurrentCulture, message, args)) =>
        StatusCode = statusCode;
}