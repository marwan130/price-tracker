namespace PriceTracker.Application.DTOs.Common;

public class ApiResponse<T>
{
    public bool      Success   { get; set; }
    public T?        Data      { get; set; }
    public string?   Message   { get; set; }
    public ApiError? Error     { get; set; }
    public DateTime  Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data    = data,
        Message = message
    };

    public static ApiResponse<T> Fail(ApiError error) => new()
    {
        Success = false,
        Error   = error
    };

    public static ApiResponse<T> Fail(string code, string message, IEnumerable<string>? details = null) => new()
    {
        Success = false,
        Error   = new ApiError
        {
            Code    = code,
            Message = message,
            Details = details ?? []
        }
    };
}