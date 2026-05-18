namespace PriceTracker.Domain.Exceptions;

public class BusinessRulesException : Exception
{
    public BusinessRulesException(string message) : base(message) {}
}