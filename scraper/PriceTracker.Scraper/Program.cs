using Microsoft.Extensions.DependencyInjection;
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
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

    builder.Services.AddSerilog();

    builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
    builder.Services.Configure<ScraperOptions>(builder.Configuration.GetSection("Scraper"));

    builder.Services.AddHttpClient<IPriceTrackerApiClient, PriceTrackerApiClient>((sp, client) =>
    {
        var api = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(
            sp.GetRequiredService<IOptions<ScraperOptions>>().Value.RequestTimeoutSeconds);
    });

    builder.Services.AddHttpClient("page-fetcher", client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (compatible; SmartPriceTracker/1.0)");
    });

    // Register store scrapers
    builder.Services.AddSingleton<IStoreScraper, HtmlStoreScraper>();
    builder.Services.AddSingleton<IStoreScraper, PlaywrightStoreScraper>();
    builder.Services.AddSingleton<IStoreScraper, ApiStoreScraper>();
    builder.Services.AddSingleton<StoreScraperFactory>();
    
    builder.Services.AddHostedService<ScrapeWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Scraper terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
