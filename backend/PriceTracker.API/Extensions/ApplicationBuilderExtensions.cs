namespace PriceTracker.API.Extensions;

using Hangfire;
using PriceTracker.API.Middleware;
using PriceTracker.Infrastructure.Jobs;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UsePriceTrackerMiddleware(this IApplicationBuilder app)
    {
        app.UseSecurityHeaders();
        app.UseMiddleware<InternalApiKeyMiddleware>();
        app.UseExceptionHandler();
        return app;
    }

    private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                var config = context.RequestServices.GetRequiredService<IConfiguration>();
                var hangfirePath = config["Hangfire:DashboardPath"] ?? "/hangfire";
                var isHangfireDashboard = context.Request.Path.StartsWithSegments(hangfirePath);

                headers.TryAdd("X-Content-Type-Options", "nosniff");
                headers.TryAdd("X-Frame-Options", "DENY");
                headers.TryAdd("Referrer-Policy", "no-referrer");
                headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
                headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
                headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");

                if (!isHangfireDashboard)
                    headers.TryAdd("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'; base-uri 'none'");

                return Task.CompletedTask;
            });

            await next();
        });
    }

    public static IApplicationBuilder UsePriceTrackerSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Price Tracker API v1");
            options.RoutePrefix = "swagger";
        });

        return app;
    }

    public static IApplicationBuilder UsePriceTrackerHangfire(
        this IApplicationBuilder app,
        IConfiguration           config)
    {
        app.UseHangfireDashboard(
            config["Hangfire:DashboardPath"] ?? "/hangfire",
            new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()]
            });

        using (var scope = app.ApplicationServices.CreateScope())
        {
            var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobs.RemoveIfExists("scrape-all-listings");
            recurringJobs.AddOrUpdate<PriceAlertJob>(
                "evaluate-price-alerts",
                job => job.ExecuteAsync(),
                "*/15 * * * *");
            
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Hangfire jobs configured: evaluate-price-alerts every 15 minutes");
        }

        return app;
    }
}
