namespace PrettyWoman.Api.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context.Request.Headers[HeaderName].ToString());
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object?>
               {
                   ["CorrelationId"] = correlationId
               }))
        {
            await next(context);
        }
    }

    private static string GetCorrelationId(string? suppliedCorrelationId)
        => !string.IsNullOrWhiteSpace(suppliedCorrelationId) && suppliedCorrelationId.Length <= 128
            ? suppliedCorrelationId
            : Guid.NewGuid().ToString("N");
}
