namespace PriceTracker.API.Middleware;

using Microsoft.AspNetCore.Diagnostics;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Domain.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext      context,
        Exception        exception,
        CancellationToken ct)
    {
        var (statusCode, code, message) = exception switch
        {
            NotFoundException      e => (404, "RESOURCE_NOT_FOUND",     e.Message),
            ConflictException      e => (409, "DUPLICATE_ENTRY",         e.Message),
            Domain.Exceptions.ValidationException e => (400, "VALIDATION_ERROR", e.Message),
            UnauthorizedException  e => (401, "INVALID_CREDENTIALS",    e.Message),
            ForbiddenException     e => (403, "INSUFFICIENT_ROLE",      e.Message),
            BusinessRuleException  e => (422, "BUSINESS_RULE_VIOLATION", e.Message),
            _                        => (500, "INTERNAL_SERVER_ERROR",  "An unexpected error occurred.")
        };

        _logger.LogError(exception, "Exception caught: {Code} - {Message}", code, message);

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(new ApiError
        {
            Code    = code,
            Message = message
        });

        await context.Response.WriteAsJsonAsync(response, ct);
        return true;
    }
}