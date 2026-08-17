using LebanonBasketballReservation.Business.Exceptions;
using System.Net;
using System.Text.Json;

namespace LebanonBasketballReservation.API.Middleware;

/// <summary>
/// Turns domain exceptions into the matching HTTP status with a JSON body, so controllers
/// do not each need their own try/catch. Unexpected exceptions become a generic 500.
/// </summary>
public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
            var (status, message) = ex switch
            {
                NotFoundException => ((int)HttpStatusCode.NotFound, ex.Message),
                ForbiddenException => ((int)HttpStatusCode.Forbidden, ex.Message),
                ConflictException => ((int)HttpStatusCode.Conflict, ex.Message),
                ValidationException => ((int)HttpStatusCode.BadRequest, ex.Message),
                UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "You are not allowed to perform this action."),
                KeyNotFoundException => ((int)HttpStatusCode.NotFound, "The requested item was not found."),
                _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            if (status == 500)
                _logger.LogError(ex, "Unhandled API exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("{Status} for {Method} {Path}: {Message}", status, context.Request.Method, context.Request.Path, ex.Message);

            if (context.Response.HasStarted) return;

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status,
                error = message,
                traceId = context.TraceIdentifier
            }));
        }
    }
}
