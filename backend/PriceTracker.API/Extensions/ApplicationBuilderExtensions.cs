namespace PriceTracker.API.Extensions;

using Hangfire;
using PriceTracker.API.Middleware;
using PriceTracker.Infrastructure.Jobs;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UsePriceTrackerMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<InternalApiKeyMiddleware>();
        app.UseExceptionHandler();
        return app;
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
        app.UseHangfireDashboard(config["Hangfire:DashboardPath"] ?? "/hangfire");

        RecurringJob.AddOrUpdate<PriceAlertJob>(
            "evaluate-price-alerts",
            job => job.ExecuteAsync(),
            "*/15 * * * *");

        return app;
    }
}