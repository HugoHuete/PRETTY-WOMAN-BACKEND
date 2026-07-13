using System.Diagnostics;
using System.Security.Claims;

namespace PrettyWoman.Api.Middlewares;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        logger.LogInformation("Solicitud iniciada {RequestMethod} {RequestPath}", method, path);

        await next(context);

        stopwatch.Stop();
        logger.LogInformation(
            "Solicitud finalizada {RequestMethod} {RequestPath} con estado {StatusCode} en {ElapsedMilliseconds} ms por usuario {UserId}",
            method,
            path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous");
    }
}
