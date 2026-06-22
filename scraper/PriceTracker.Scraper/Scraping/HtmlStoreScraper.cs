namespace PriceTracker.Scraper.Scraping;

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;

/// <summary>
/// HTML-based scraper for static websites using AngleSharp.
/// Extracts prices from JSON-LD, meta tags, and DOM attributes.
/// </summary>
public class HtmlStoreScraper : IStoreScraper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly Regex PricePattern = new(
        @"(\d{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?|\d+(?:\.\d{1,2})?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string ScraperType => "Html";

    public HtmlStoreScraper(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public bool CanHandle(string url)
    {
        // By default, can handle any URL - this is the fallback scraper
        return true;
    }

    public async Task<ScrapeResult?> ScrapeAsync(string url, string? currencyCode = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var client = _httpClientFactory.CreateClient("page-fetcher");
        
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        
        using var response = await client.SendAsync(request, cts.Token);
        
        if (!response.IsSuccessStatusCode)
        {
            // Check for CAPTCHA indicators in status code
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new ScrapeResult
                {
                    Price = 0,
                    CurrencyCode = currencyCode ?? "USD",
                    IsCaptchaBlocked = true,
                    BlockReason = "Access forbidden (likely CAPTCHA or bot detection)"
                };
            }
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return new ScrapeResult
                {
                    Price = 0,
                    CurrencyCode = currencyCode ?? "USD",
                    IsCaptchaBlocked = true,
                    BlockReason = "Rate limited (too many requests)"
                };
            }
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(ct);
        
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        // Check for CAPTCHA indicators in HTML
        if (IsCaptchaBlocked(html))
        {
            return new ScrapeResult
            {
                Price = 0,
                CurrencyCode = currencyCode ?? "USD",
                IsCaptchaBlocked = true,
                BlockReason = "CAPTCHA detected in page content"
            };
        }

        return ExtractFromHtml(html, currencyCode);
    }

    private static bool IsCaptchaBlocked(string html)
    {
        var lowerHtml = html.ToLowerInvariant();
        
        // Common CAPTCHA indicators
        var captchaIndicators = new[]
        {
            "captcha",
            "recaptcha",
            "hcaptcha",
            "cf-challenge",
            "cloudflare",
            "access denied",
            "you have been blocked",
            "please verify you are a human",
            "are you a robot",
            "security check",
            "bot detection"
        };

        foreach (var indicator in captchaIndicators)
        {
            if (lowerHtml.Contains(indicator))
            {
                return true;
            }
        }

        return false;
    }

    private ScrapeResult? ExtractFromHtml(string html, string? fallbackCurrencyCode)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var fromJsonLd = TryExtractFromJsonLd(document.QuerySelectorAll("script[type='application/ld+json']"));
        if (fromJsonLd is not null)
            return fromJsonLd;

        var fromMeta = TryExtractFromMeta(document);
        if (fromMeta is not null)
            return fromMeta;

        var fromDom = TryExtractFromDomAttributes(document);
        if (fromDom is not null)
            return fromDom;

        if (TryParsePrice(document.Body?.TextContent, out var price))
        {
            return new ScrapeResult
            {
                Price = price,
                CurrencyCode = fallbackCurrencyCode ?? "USD"
            };
        }

        return null;
    }

    private static ScrapeResult? TryExtractFromJsonLd(IEnumerable<AngleSharp.Dom.IElement> scripts)
    {
        foreach (var script in scripts)
        {
            if (string.IsNullOrWhiteSpace(script.TextContent))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(script.TextContent);
                if (TryFindPriceInJson(doc.RootElement, out var price, out var currency))
                {
                    return new ScrapeResult
                    {
                        Price = price,
                        CurrencyCode = currency ?? "USD"
                    };
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return null;
    }

    private static bool TryFindPriceInJson(JsonElement element, out decimal price, out string? currency)
    {
        price = 0;
        currency = null;

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindPriceInJson(item, out price, out currency))
                    return true;
            }

            return false;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return false;

        if (element.TryGetProperty("priceCurrency", out var currencyEl))
            currency = currencyEl.GetString();

        if (element.TryGetProperty("offers", out var offers))
        {
            if (offers.ValueKind == JsonValueKind.Array)
            {
                foreach (var offer in offers.EnumerateArray())
                {
                    if (TryReadPriceProperty(offer, ref price, ref currency))
                        return true;
                }
            }
            else if (TryReadPriceProperty(offers, ref price, ref currency))
            {
                return true;
            }
        }

        if (TryReadPriceProperty(element, ref price, ref currency))
            return true;

        foreach (var property in element.EnumerateObject())
        {
            if (TryFindPriceInJson(property.Value, out price, out currency))
                return true;
        }

        return false;
    }

    private static bool TryReadPriceProperty(JsonElement element, ref decimal price, ref string? currency)
    {
        if (element.TryGetProperty("priceCurrency", out var currencyEl))
            currency = currencyEl.GetString();

        foreach (var name in new[] { "price", "lowPrice", "highPrice" })
        {
            if (!element.TryGetProperty(name, out var priceEl))
                continue;

            if (TryParseJsonPrice(priceEl, out var parsed))
            {
                price = parsed;
                return true;
            }
        }

        return false;
    }

    private static bool TryParseJsonPrice(JsonElement element, out decimal price)
    {
        price = 0;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out price))
            return true;

        if (element.ValueKind == JsonValueKind.String && TryParsePrice(element.GetString(), out price))
            return true;

        return false;
    }

    private static ScrapeResult? TryExtractFromMeta(AngleSharp.Dom.IDocument document)
    {
        var amount = GetMetaContent(document, "product:price:amount")
                  ?? GetMetaContent(document, "og:price:amount");

        if (!TryParsePrice(amount, out var price))
            return null;

        var currency = GetMetaContent(document, "product:price:currency")
                    ?? GetMetaContent(document, "og:price:currency")
                    ?? "USD";

        return new ScrapeResult { Price = price, CurrencyCode = currency };
    }

    private static string? GetMetaContent(AngleSharp.Dom.IDocument document, string property)
    {
        var meta = document.QuerySelector($"meta[property='{property}'], meta[name='{property}']");
        return meta?.GetAttribute("content");
    }

    private static ScrapeResult? TryExtractFromDomAttributes(AngleSharp.Dom.IDocument document)
    {
        foreach (var selector in new[] { "[data-price]", "[itemprop='price']", ".price", "#price" })
        {
            var element = document.QuerySelector(selector);
            if (element is null)
                continue;

            var raw = element.GetAttribute("content")
                   ?? element.GetAttribute("data-price")
                   ?? element.TextContent;

            if (TryParsePrice(raw, out var price))
            {
                return new ScrapeResult
                {
                    Price = price,
                    CurrencyCode = "USD"
                };
            }
        }

        return null;
    }

    private static bool TryParsePrice(string? raw, out decimal price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var match = PricePattern.Match(raw);
        if (!match.Success)
            return false;

        var normalized = match.Groups[1].Value.Replace(" ", string.Empty).Replace(",", string.Empty);
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price)
            && price > 0;
    }
}
