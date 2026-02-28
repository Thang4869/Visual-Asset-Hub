using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VAH.Backend.Exceptions;

namespace VAH.Backend.Middleware;

/// <summary>
/// Global exception handler implementing <see cref="IExceptionHandler"/> (ASP.NET Core 8+).
/// Produces RFC 7807 ProblemDetails responses for all unhandled exceptions.
/// Replaces the legacy ExceptionHandlingMiddleware.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var problemDetails = exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = notFound.Message
            },

            Exceptions.ValidationException validation => CreateValidationProblem(validation),

            ArgumentException argument => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = argument.Message
            },

            KeyNotFoundException keyNotFound => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = keyNotFound.Message
            },

            UnauthorizedAccessException => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Authentication is required."
            },

            InvalidOperationException conflict => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = conflict.Message
            },

            _ => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred."
            }
        };

        // Always include traceId per RFC 7807 extensions
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Exception handled — stop pipeline propagation
    }

    /// <summary>
    /// Build a validation ProblemDetails with per-field error map in the extensions.
    /// </summary>
    private static ProblemDetails CreateValidationProblem(Exceptions.ValidationException ex)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "Validation Failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = ex.Message
        };

        if (ex.Errors.Count > 0)
            problem.Extensions["errors"] = ex.Errors;

        return problem;
    }
}
