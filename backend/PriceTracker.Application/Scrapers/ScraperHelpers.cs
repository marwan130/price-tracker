namespace PriceTracker.Application.Scrapers;

using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;

public static class ScraperHelpers
{
    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.4; rv:126.0) Gecko/20100101 Firefox/126.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4.1 Safari/605.1.15",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 OPR/111.0.0.0",
    ];

    private static readonly string[] BlockIndicators =
    [
        "captcha", "recaptcha", "hcaptcha", "cf-challenge",
        "cloudflare", "access denied", "you have been blocked",
        "please verify you are a human", "are you a robot",
        "security check", "bot detection", "403 forbidden",
    ];

    private static readonly Random Rng = new();

    private static readonly Regex PriceRegex = new(
        @"(\d{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?|\d+(?:\.\d{1,2})?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly HtmlParser HtmlParser = new();

    public static string RandomUserAgent() => UserAgents[Rng.Next(UserAgents.Length)];

    public static bool IsBlocked(string html)
    {
        foreach (var indicator in BlockIndicators)
            if (html.Contains(indicator, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    public static Task PoliteDelayAsync(CancellationToken ct, int minMs = 400, int maxMs = 1200)
        => Task.Delay(Rng.Next(minMs, maxMs), ct);

    public static async Task<string?> FetchHtmlAsync(
        IHttpClientFactory factory, string url, CancellationToken ct, int timeoutSecs = 10)
    {
        var client = factory.CreateClient("product-search");
        return await SendWithRetryAsync(async () =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", RandomUserAgent());
            req.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            req.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            req.Headers.Add("DNT", "1");
            req.Headers.Add("Referer", url);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSecs));
            return await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        }, ct);
    }

    public static async Task<string?> SendAsync(
        HttpClient client, HttpRequestMessage req, CancellationToken ct, int timeoutSecs = 10)
    {
        if (!req.Headers.Contains("User-Agent"))
            req.Headers.TryAddWithoutValidation("User-Agent", RandomUserAgent());

        return await SendWithRetryAsync(async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSecs));
            return await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        }, ct);
    }

    private static async Task<string?> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> send, CancellationToken ct, int maxAttempts = 3)
    {
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            HttpResponseMessage? resp = null;
            try
            {
                resp = await send();

                if (resp.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                {
                    var retryAfter = resp.Headers.RetryAfter?.Delta ?? delay;
                    await Task.Delay(retryAfter, ct);
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
                    resp.Dispose();
                    continue;
                }

                if (!resp.IsSuccessStatusCode) return null;

                var html = await resp.Content.ReadAsStringAsync(ct);
                return IsBlocked(html) ? null : html;
            }
            catch (OperationCanceledException) { return null; }
            catch (HttpRequestException) when (attempt < maxAttempts - 1)
            {
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
            catch { return null; }
            finally { resp?.Dispose(); }
        }

        return null;
    }

    public static bool TryParsePrice(string? raw, out decimal price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var m = PriceRegex.Match(raw);
        if (!m.Success) return false;
        var normalized = m.Groups[1].Value.Replace(" ", "").Replace(",", "");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price) && price > 0;
    }

    public static string NormalizeProductUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim();
        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    public static JsonElement FindArray(JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Undefined) return default;
        if (el.TryGetProperty(name, out var prop)) return prop;
        if (el.ValueKind != JsonValueKind.Object) return default;
        foreach (var child in el.EnumerateObject())
        {
            var found = FindArray(child.Value, name);
            if (found.ValueKind != JsonValueKind.Undefined) return found;
        }
        return default;
    }

    public static string? GetJsonString(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var val)) continue;
            if (val.ValueKind == JsonValueKind.String) return val.GetString()?.Trim();
            if (val.ValueKind == JsonValueKind.Number) return val.GetDecimal().ToString();
        }
        return null;
    }

    public static string? GetJsonImage(JsonElement el)
    {
        if (!el.TryGetProperty("image", out var image)) return null;
        if (image.ValueKind == JsonValueKind.String) return image.GetString()?.Trim();
        if (image.ValueKind == JsonValueKind.Array)
            return image.EnumerateArray()
                .Select(i => i.ValueKind == JsonValueKind.String ? i.GetString()?.Trim() : null)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        return image.ValueKind == JsonValueKind.Object ? GetJsonString(image, "url", "contentUrl") : null;
    }

    public static string InferStoreName(string url)
    {
        if (url.Contains("amazon.eg",             StringComparison.OrdinalIgnoreCase)) return "Amazon Egypt";
        if (url.Contains("amazon.sa",             StringComparison.OrdinalIgnoreCase)) return "Amazon Saudi Arabia";
        if (url.Contains("amazon.ae",             StringComparison.OrdinalIgnoreCase)) return "Amazon UAE";
        if (url.Contains("noon.com/egypt",        StringComparison.OrdinalIgnoreCase)) return "Noon Egypt";
        if (url.Contains("noon.com/saudi",        StringComparison.OrdinalIgnoreCase)) return "Noon Saudi Arabia";
        if (url.Contains("noon.com/uae",          StringComparison.OrdinalIgnoreCase)) return "Noon UAE";
        if (url.Contains("jumia.com.eg",          StringComparison.OrdinalIgnoreCase)) return "Jumia Egypt";
        if (url.Contains("jumia.com.sa",          StringComparison.OrdinalIgnoreCase)) return "Jumia Saudi Arabia";
        if (url.Contains("namshi.com",            StringComparison.OrdinalIgnoreCase)) return "Namshi";
        if (url.Contains("carrefouregypt.com",    StringComparison.OrdinalIgnoreCase)) return "Carrefour Egypt";
        if (url.Contains("carrefourksa.com",      StringComparison.OrdinalIgnoreCase)) return "Carrefour KSA";
        if (url.Contains("carrefouruae.com",      StringComparison.OrdinalIgnoreCase)) return "Carrefour UAE";
        if (url.Contains("ikea.com/eg",           StringComparison.OrdinalIgnoreCase)) return "IKEA Egypt";
        if (url.Contains("ikea.com/sa",           StringComparison.OrdinalIgnoreCase)) return "IKEA Saudi Arabia";
        if (url.Contains("ikea.com/ae",           StringComparison.OrdinalIgnoreCase)) return "IKEA UAE";
        if (url.Contains("homzmart.com",          StringComparison.OrdinalIgnoreCase)) return "Homzmart";
        if (url.Contains("homecentre.com/sa",     StringComparison.OrdinalIgnoreCase)) return "Home Centre KSA";
        if (url.Contains("homecentre.com/ae",     StringComparison.OrdinalIgnoreCase)) return "Home Centre UAE";
        if (url.Contains("ubuy.com.eg",           StringComparison.OrdinalIgnoreCase)) return "Ubuy Egypt";
        if (url.Contains("ubuy.com.sa",           StringComparison.OrdinalIgnoreCase)) return "Ubuy Saudi Arabia";
        if (url.Contains("ubuy.ae",               StringComparison.OrdinalIgnoreCase)) return "Ubuy UAE";
        if (url.Contains("sharafdg.com",          StringComparison.OrdinalIgnoreCase)) return "Sharaf DG";
        if (url.Contains("geekay.com",            StringComparison.OrdinalIgnoreCase)) return "Geekay UAE";
        if (url.Contains("faces.eg",              StringComparison.OrdinalIgnoreCase)) return "Faces Egypt";
        if (url.Contains("faces.ae",              StringComparison.OrdinalIgnoreCase)) return "Faces UAE";
        if (url.Contains("faces.com",             StringComparison.OrdinalIgnoreCase)) return "Faces KSA";
        if (url.Contains("watsons.ae",            StringComparison.OrdinalIgnoreCase)) return "Watsons UAE";
        if (url.Contains("watsons.sa",            StringComparison.OrdinalIgnoreCase)) return "Watsons KSA";
        if (url.Contains("goldenscent.com",       StringComparison.OrdinalIgnoreCase)) return "Golden Scent";
        if (url.Contains("niceonesa.com",         StringComparison.OrdinalIgnoreCase)) return "Nice One";
        if (url.Contains("jarir.com",             StringComparison.OrdinalIgnoreCase)) return "Jarir Bookstore";
        if (url.Contains("extra.com",             StringComparison.OrdinalIgnoreCase)) return "eXtra";
        if (url.Contains("btech.com",             StringComparison.OrdinalIgnoreCase)) return "B.TECH";
        if (url.Contains("jumbo.ae",              StringComparison.OrdinalIgnoreCase)) return "Jumbo Electronics";
        if (url.Contains("emaxonline.com",        StringComparison.OrdinalIgnoreCase)) return "Emax";
        if (url.Contains("sssports.com",          StringComparison.OrdinalIgnoreCase)) return "Sun & Sand Sports";
        if (url.Contains("decathlon.ae",          StringComparison.OrdinalIgnoreCase)) return "Decathlon UAE";
        return "Online Store";
    }

    public static string InferCurrencyCode(string url)
    {
        if (url.Contains("amazon.sa",      StringComparison.OrdinalIgnoreCase)
         || url.Contains("noon.com/saudi", StringComparison.OrdinalIgnoreCase)) return "SAR";
        if (url.Contains("amazon.ae",      StringComparison.OrdinalIgnoreCase)
         || url.Contains("noon.com/uae",   StringComparison.OrdinalIgnoreCase)) return "AED";
        return "EGP";
    }

    public static string InferCategoryName(string name, string url)
    {
        var h = $"{name} {url}".ToLowerInvariant();
        if (ContainsAny(h, "iphone", "samsung galaxy", "smartphone", "mobile phone")) return "Mobile Phones";
        if (ContainsAny(h, "laptop", "notebook", "macbook", "thinkpad"))             return "Laptops";
        if (ContainsAny(h, "tablet", "ipad"))                                         return "Tablets";
        if (ContainsAny(h, "headphone", "earbud", "airpods", "speaker"))             return "Audio";
        if (ContainsAny(h, "tv", "television", "monitor", "display"))                return "TVs & Monitors";
        if (ContainsAny(h, "watch", "smartwatch"))                                   return "Wearables";
        if (ContainsAny(h, "shoe", "shirt", "dress", "jeans", "jacket", "fashion")) return "Fashion";
        if (ContainsAny(h, "fridge", "refrigerator", "washer", "microwave"))        return "Home Appliances";
        if (ContainsAny(h, "sofa", "chair", "table", "bed", "furniture"))           return "Furniture";
        if (ContainsAny(h, "makeup", "perfume", "skincare", "beauty"))              return "Beauty";
        return "General";
    }

    public sealed record StructuredProduct(
        string? Name, string? Description, string? ImageUrl, decimal Price, string? Currency);

    public static StructuredProduct? ExtractStructuredProduct(IEnumerable<string> scriptBodies)
    {
        foreach (var body in scriptBodies.Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            try
            {
                using var json = JsonDocument.Parse(body);
                var product    = FindProductObject(json.RootElement);
                if (product.HasValue) return MapStructuredProduct(product.Value);
            }
            catch (JsonException) { continue; }
        }
        return null;
    }

    private static JsonElement? FindProductObject(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            if (IsProductType(el)) return el;
            foreach (var prop in el.EnumerateObject())
            {
                var n = FindProductObject(prop.Value);
                if (n.HasValue) return n;
            }
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                var n = FindProductObject(item);
                if (n.HasValue) return n;
            }
        }
        return null;
    }

    private static bool IsProductType(JsonElement el)
    {
        if (!el.TryGetProperty("@type", out var t)) return false;
        return t.ValueKind switch
        {
            JsonValueKind.String => string.Equals(t.GetString(), "Product", StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Array  => t.EnumerateArray().Any(x =>
                x.ValueKind == JsonValueKind.String &&
                string.Equals(x.GetString(), "Product", StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static StructuredProduct MapStructuredProduct(JsonElement product)
    {
        var offer    = product.TryGetProperty("offers", out var offers) ? FirstObject(offers) : null;
        var priceStr = offer.HasValue ? GetJsonString(offer.Value, "price", "lowPrice", "highPrice") : null;
        decimal.TryParse(priceStr?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
        return new StructuredProduct(
            GetJsonString(product, "name"),
            GetJsonString(product, "description"),
            GetJsonImage(product),
            price,
            offer.HasValue ? GetJsonString(offer.Value, "priceCurrency") : null);
    }

    private static JsonElement? FirstObject(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Object) return el;
        if (el.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in el.EnumerateArray())
            if (item.ValueKind == JsonValueKind.Object) return item;
        return null;
    }

    private static bool ContainsAny(string value, params string[] needles)
        => needles.Any(value.Contains);
}