namespace PriceTracker.Application.Scrapers;

public enum ScraperKind
{
    AmazonHtml,
    NoonApi,
    JumiaHtml,
    NamshiApi,
    GenericHtml,
}

public sealed record StoreDescriptor(
    string      Name,
    string      Currency,
    string      Country,
    ScraperKind Kind,
    string      SearchUrlTemplate,
    string?     ApiKey = null,
    string?     AppId  = null);