namespace LebanonBasketballReservation.Business.Exceptions;

/// <summary>
/// Base for errors caused by the user's request rather than a defect. These carry messages
/// that are safe to show, so controllers can surface <see cref="Exception.Message"/> directly
/// while unexpected exceptions stay hidden behind a generic error page.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>The requested entity does not exist.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException For(string entity, object id)
        => new($"{entity} '{id}' was not found.");
}

/// <summary>The caller is authenticated but not allowed to act on this entity.</summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You are not allowed to perform this action.") : base(message) { }
}

/// <summary>The request is well-formed but conflicts with the current state (e.g. slot already taken).</summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>The request failed validation.</summary>
public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
        => Errors = new Dictionary<string, string[]>();

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors) : base(message)
        => Errors = errors;
}
