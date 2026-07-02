namespace PriceTracker.API.Middleware;

using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var config      = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config["Hangfire:DashboardApiKey"];

        if (string.IsNullOrWhiteSpace(expectedKey))
            return false;

        if (httpContext.Request.Headers.TryGetValue("X-Dashboard-Key", out var headerKey)
            && FixedTimeEquals(headerKey.ToString(), expectedKey))
        {
            return true;
        }

        return httpContext.Request.Query.TryGetValue("dashboardKey", out var queryKey)
            && FixedTimeEquals(queryKey.ToString(), expectedKey);
    }

    private static bool FixedTimeEquals(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
