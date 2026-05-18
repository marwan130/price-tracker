namespace PriceTracker.Domain.Exceptions;

public class ValidationException : Exception
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(string message) : base(message) => Errors = [message];

    public ValidationException(IEnumerable<string> errors) : base("One or more validation errors occurred.") => Errors = errors;
}