using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Exceptions;

namespace PrettyWoman.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppExceptionBase ex)
        {
            await WriteProblemDetailsAsync(context, (int)ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Excepción no controlada al procesar {RequestMethod} {RequestPath} con correlación {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado.");
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int status, string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = GetTitle(status),
            Status = status,
            Detail = detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problemDetails,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            context.RequestAborted);
    }

    private static string GetTitle(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Solicitud inválida",
        StatusCodes.Status401Unauthorized => "No autorizado",
        StatusCodes.Status403Forbidden => "Acceso denegado",
        StatusCodes.Status404NotFound => "Recurso no encontrado",
        StatusCodes.Status415UnsupportedMediaType => "Tipo de contenido no compatible",
        StatusCodes.Status500InternalServerError => "Error interno del servidor",
        _ => "Error de solicitud"
    };
}
