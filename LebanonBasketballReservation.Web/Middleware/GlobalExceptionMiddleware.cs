using LebanonBasketballReservation.Business.Exceptions;
using System.Net;
using System.Text.Json;

namespace LebanonBasketballReservation.Web.Middleware;

/// <summary>
/// Converts unhandled exceptions into a sensible response. Domain exceptions carry
/// user-safe messages and map to specific status codes; everything else becomes a
/// generic 500 so internal details never reach the browser.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            var (statusCode, message) = Describe(ex);

            if (statusCode == (int)HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("{Status} for {Method} {Path}: {Message}", statusCode, context.Request.Method, context.Request.Path, ex.Message);

            // Once the response has begun there is nothing left to rewrite.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started; cannot write error page");
                return;
            }

            context.Response.Clear();

            if (WantsJson(context))
            {
                // API and AJAX callers get the real status code and a machine-readable body.
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message, status = statusCode }));
                return;
            }

            // Browsers get a redirect to the friendly page. Redirect() sets 302 itself — setting
            // the error code here instead would send a 500 with a Location header, which renders blank.
            context.Response.Redirect($"/Home/Error?code={statusCode}");
        }
    }

    /// <summary>Maps an exception to a status code and a message that is safe to display.</summary>
    private static (int StatusCode, string Message) Describe(Exception ex) => ex switch
    {
        NotFoundException => ((int)HttpStatusCode.NotFound, ex.Message),
        ForbiddenException => ((int)HttpStatusCode.Forbidden, ex.Message),
        ConflictException => ((int)HttpStatusCode.Conflict, ex.Message),
        Business.Exceptions.ValidationException => ((int)HttpStatusCode.BadRequest, ex.Message),
        UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "You are not allowed to perform this action."),
        KeyNotFoundException => ((int)HttpStatusCode.NotFound, "The requested item was not found."),
        _ => ((int)HttpStatusCode.InternalServerError, "Something went wrong on our side. Please try again.")
    };

    private static bool WantsJson(HttpContext context)
        => context.Request.Path.StartsWithSegments("/api")
        || context.Request.Headers.Accept.Any(v => v is not null && v.Contains("application/json"))
        || string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
