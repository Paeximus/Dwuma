using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Infrastructure;

public sealed class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode;
        string title;

        switch (exception)
        {
            case ArgumentException:
                statusCode =
                    StatusCodes.Status400BadRequest;
                title = "Invalid request";
                break;

            case KeyNotFoundException:
                statusCode =
                    StatusCodes.Status404NotFound;
                title = "Resource not found";
                break;

            case UnauthorizedAccessException:
                statusCode =
                    StatusCodes.Status401Unauthorized;
                title = "Unauthorized";
                break;

            case InvalidOperationException:
                statusCode =
                    StatusCodes.Status409Conflict;
                title = "Operation could not be completed";
                break;

            default:
                statusCode =
                    StatusCodes.Status500InternalServerError;
                title = "Unexpected server error";
                break;
        }

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred while processing {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed with status {StatusCode} for {Method} {Path}.",
                statusCode,
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        var problem =
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail =
                    statusCode ==
                    StatusCodes.Status500InternalServerError
                        ? "An unexpected error occurred. Please try again."
                        : exception.Message,
                Instance =
                    httpContext.Request.Path
            };

        problem.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        httpContext.Response.StatusCode =
            statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }
}