namespace PriceTracker.API.Middleware;

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
        if (context.Request.Path.StartsWithSegments("/v1/price-history") &&
            context.Request.Method == "POST" ||
            context.Request.Path.StartsWithSegments("/v1/scrape-logs") &&
            context.Request.Method == "POST")
        {
            if (!context.Request.Headers.TryGetValue("X-Internal-Key", out var key) ||
                key != _config["InternalApi:Key"])
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid or missing internal API key." });
                return;
            }
        }

        await _next(context);
    }
}