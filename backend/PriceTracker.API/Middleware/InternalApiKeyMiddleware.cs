namespace PriceTracker.API.Middleware;

using System.Security.Cryptography;
using System.Text;

public class InternalApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration  _config;

    public InternalApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next   = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresInternalKey(context))
        {
            var expectedKey = _config["InternalApi:Key"];

            if (string.IsNullOrWhiteSpace(expectedKey) ||
                !context.Request.Headers.TryGetValue("X-Internal-Key", out var key) ||
                !FixedTimeEquals(key.ToString(), expectedKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid or missing internal API key." });
                return;
            }
        }

        await _next(context);
    }

    private static bool RequiresInternalKey(HttpContext context)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments("/v1/internal"))
            return true;

        if (context.Request.Method != HttpMethods.Post)
            return false;

        return path.StartsWithSegments("/v1/price-history")
            || path.StartsWithSegments("/v1/scrape-logs");
    }

    private static bool FixedTimeEquals(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
