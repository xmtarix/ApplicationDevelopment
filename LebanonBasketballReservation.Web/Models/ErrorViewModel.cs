namespace LebanonBasketballReservation.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int StatusCode { get; set; } = 500;

    public string Title => StatusCode switch
    {
        400 => "Bad request",
        403 => "Access denied",
        404 => "Page not found",
        409 => "That didn't work",
        _ => "Something went wrong"
    };

    public string Message => StatusCode switch
    {
        400 => "We couldn't understand that request. Please check the details and try again.",
        403 => "You don't have permission to view this page.",
        404 => "The page you're looking for doesn't exist or has moved.",
        409 => "This action conflicts with the current state. Refresh and try again.",
        _ => "An unexpected error occurred on our side. Please try again in a moment."
    };

    public string Icon => StatusCode switch
    {
        403 => "bi-shield-lock",
        404 => "bi-compass",
        _ => "bi-exclamation-triangle"
    };
}
