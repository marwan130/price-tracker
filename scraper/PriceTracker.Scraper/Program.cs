using Microsoft.Extensions.Options;
using PriceTracker.Scraper.Api;
using PriceTracker.Scraper.Configuration;
using PriceTracker.Scraper.Scraping;
using PriceTracker.Scraper.Workers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            services.Configure<ApiOptions>(context.Configuration.GetSection("Api"));
            services.Configure<ScraperOptions>(context.Configuration.GetSection("Scraper"));

            services.AddHttpClient<IPriceTrackerApiClient, PriceTrackerApiClient>((sp, client) =>
            {
                var api = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(
                    sp.GetRequiredService<IOptions<ScraperOptions>>().Value.RequestTimeoutSeconds);
            });

            services.AddHttpClient("page-fetcher", client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (compatible; SmartPriceTracker/1.0)");
            });

            services.AddSingleton<IPriceExtractor, HtmlPriceExtractor>();
            services.AddHostedService<ScrapeWorker>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Scraper terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
