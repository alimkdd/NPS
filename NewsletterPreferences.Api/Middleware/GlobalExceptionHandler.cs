using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace NewsletterPreferences.Api.Middleware;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.GetCorrelationId() ?? httpContext.TraceIdentifier;
        var timestamp = DateTime.UtcNow;
        var sanitizedMessage = Sanitize(exception);
        var sanitizedInner = exception.InnerException is null ? null : Sanitize(exception.InnerException);

        logger.LogError(exception,
            "Unhandled exception. CorrelationId={CorrelationId} Timestamp={Timestamp:o} " +
            "Method={Method} Path={Path} QueryString={QueryString} " +
            "ExceptionType={ExceptionType} Message={ExceptionMessage} " +
            "InnerExceptionType={InnerType} InnerMessage={InnerMessage}",
            correlationId,
            timestamp,
            httpContext.Request.Method,
            httpContext.Request.Path,
            QueryStringRedactor.Redact(httpContext.Request.QueryString.Value),
            exception.GetType().FullName,
            sanitizedMessage,
            exception.InnerException?.GetType().FullName,
            sanitizedInner);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var body = new Dictionary<string, object?>
        {
            ["error"] = "An unexpected error occurred. Please try again.",
            ["correlationId"] = correlationId,
            ["timestamp"] = timestamp.ToString("o")
        };

        if (environment.IsDevelopment())
        {
            body["exceptionType"] = exception.GetType().FullName;
            body["message"] = sanitizedMessage;
            body["stackTrace"] = exception.StackTrace;
            if (exception.InnerException is { } inner)
            {
                body["innerException"] = new
                {
                    type = inner.GetType().FullName,
                    message = sanitizedInner,
                    stackTrace = inner.StackTrace
                };
            }
        }

        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }

    private static string Sanitize(Exception ex) => ex switch
    {
        DbUpdateException => "Database update failed.",
        _ => ex.Message
    };
}