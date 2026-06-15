namespace PriceTracker.API.Middleware;

using Hangfire.Dashboard;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var config      = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config["Hangfire:DashboardApiKey"];

        if (environment.IsDevelopment() && string.IsNullOrWhiteSpace(expectedKey))
            return true;

        if (string.IsNullOrWhiteSpace(expectedKey))
            return false;

        if (httpContext.Request.Headers.TryGetValue("X-Dashboard-Key", out var headerKey)
            && headerKey == expectedKey)
            return true;

        return httpContext.Request.Query.TryGetValue("dashboardKey", out var queryKey)
            && queryKey == expectedKey;
    }
}
