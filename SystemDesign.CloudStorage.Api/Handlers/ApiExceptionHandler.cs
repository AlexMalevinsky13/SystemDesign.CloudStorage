using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SystemDesign.CloudStorage.Application.Exceptions;

namespace SystemDesign.CloudStorage.Api.Handlers;

/// <summary>
/// Maps expected errors to RFC 7807 responses without exposing stack traces.
/// </summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problems) : IExceptionHandler
{
    /// <summary>
    /// Handles an exception.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var app = exception as AppException;
        
        var status = app?.StatusCode ?? 500;
        if (status == 500)
            logger.LogError(exception, "Unhandled request exception {TraceId}", context.TraceIdentifier);

        context.Response.StatusCode = status;

        return await problems.TryWriteAsync(new()
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = app?.Code ?? "internal_error",
                Detail = app?.Message ?? "Internal Server Error.",
                Extensions = { { "traceId", context.TraceIdentifier } }
            }
        });
    }
}