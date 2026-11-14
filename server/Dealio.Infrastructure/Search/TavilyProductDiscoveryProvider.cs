namespace Dealio.Infrastructure.Search;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Dealio.Application.DTOs.Products;
using Dealio.Application.Interfaces.Services;
using Dealio.Application.Scrapers;
using Dealio.Domain.Markets;

public sealed class TavilyDiscoveryOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public int MonthlyQuota { get; set; } = 1_000;
    public int MaxResults { get; set; } = 20;
}

public sealed class TavilyProductDiscoveryProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<TavilyDiscoveryOptions> options,
    IConnectionMultiplexer? redis = null) : IProductDiscoveryProvider
{
    private static readonly IReadOnlyDictionary<string, string[]> MarketRetailers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["AE"] = ["amazon.ae", "noon.com", "namshi.com", "ounass.com", "sharafdg.com", "geekay.com", "sporter.com", "mumzworld.com", "jumbo.ae", "emaxonline.com", "virginmegastore.ae", "pluginsuae.com", "carrefouruae.com", "luluhypermarket.com", "ubuy.ae", "homzmart.com"],
        ["BH"] = ["amazon.ae", "noon.com", "namshi.com", "ounass.com", "geekay.com", "sporter.com", "mumzworld.com"],
        ["EG"] = ["amazon.eg", "noon.com", "namshi.com", "geekay.com", "sporter.com", "mumzworld.com", "jumia.com.eg", "btech.com", "2b.com.eg", "rayashop.com", "dream2000.com.eg", "elarabygroup.com", "sigma-computer.com", "compume.com", "carrefouregypt.com", "ubuy.com.eg"],
        ["JO"] = ["smartbuy.jo", "dna.jo", "ubuy.com", "geekay.com", "sporter.com"],
        ["KW"] = ["amazon.ae", "noon.com", "namshi.com", "ounass.com", "xcite.com", "geekay.com", "sporter.com", "mumzworld.com", "ubuy.com.kw"],
        ["OM"] = ["amazon.ae", "noon.com", "namshi.com", "ounass.com", "geekay.com", "sporter.com", "mumzworld.com"],
        ["QA"] = ["amazon.ae", "noon.com", "namshi.com", "ounass.com", "geekay.com", "sporter.com", "mumzworld.com"],
        ["SA"] = ["amazon.sa", "noon.com", "namshi.com", "ounass.com", "sharafdg.com", "geekay.com", "sporter.com", "mumzworld.com", "jumia.com.sa", "jarir.com", "extra.com", "saco.sa", "xcite.com", "carrefourksa.com", "ubuy.com.sa", "luluhypermarket.com"],
        ["TR"] = ["amazon.com.tr", "trendyol.com", "hepsiburada.com", "n11.com"]
    };
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private const int DomainsPerPage = 4;
    private const string ProviderName = "WebSearch";
    private const string ReserveQuotaScript = """
        local used = tonumber(redis.call('GET', KEYS[1]) or '0')
        local quota = tonumber(ARGV[1])
        if used >= quota then return 0 end
        local next = redis.call('INCR', KEYS[1])
        if next == 1 then redis.call('EXPIREAT', KEYS[1], ARGV[2]) end
        return next
        """;
    private readonly SemaphoreSlim _quotaLock = new(1, 1);
    private readonly ConcurrentDictionary<string, (DateTime ExpiresAtUtc, ProductSearchResult[] Results)> _localCache = new(StringComparer.Ordinal);
    private DateOnly _quotaMonth = DateOnly.FromDateTime(DateTime.UtcNow);
    private int _monthCalls;

    public string Name => ProviderName;

    public bool HasMore(string? market, int page)
        => !string.IsNullOrWhiteSpace(options.Value.ApiKey)
            && page + 1 < (int)Math.Ceiling(GetRetailerDomains(market).Length / (double)DomainsPerPage);

    public async Task<IReadOnlyList<ProductSearchResult>> SearchAsync(string query, string? market, CancellationToken cancellationToken, decimal? minPrice = null, decimal? maxPrice = null, int page = 0, string? categoryName = null)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey)) return [];
        var retailerDomains = GetRetailerDomains(market).Skip(Math.Max(0, page) * DomainsPerPage).Take(DomainsPerPage).ToArray();
        if (retailerDomains.Length == 0) return [];
        var cacheKey = BuildCacheKey(query, market, categoryName, page);
        var cached = await ReadCacheAsync(cacheKey);
        if (cached is not null)
            return cached.Where(candidate => Uri.TryCreate(candidate.ProductUrl, UriKind.Absolute, out var cachedUri)
                && IsRetailerHost(cachedUri.Host, retailerDomains)
                && !IsBlockedHost(cachedUri.Host)).ToArray();
        await ReserveQuotaAsync(cancellationToken);

        var marketName = MiddleEastMarkets.Find(market)?.Country;
        var queryText = string.Join(' ', new[] { query.Trim(), categoryName?.Trim(), marketName, "buy price" }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/search")
        {
            Content = JsonContent.Create(new
            {
                query = queryText,
                search_depth = "basic",
                max_results = Math.Clamp(options.Value.MaxResults, 1, 20),
                include_answer = false,
                include_raw_content = false,
                include_images = true,
                include_image_descriptions = true,
                include_domains = retailerDomains,
                topic = "general"
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        using var response = await httpClientFactory.CreateClient("product-discovery").SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new ProviderQuotaExceededException(Name, _quotaMonth);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{Name} returned {(int)response.StatusCode}.", null, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TavilyResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("Product discovery provider returned an empty response.");
        var results = body.Results
                .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo)
                && !IsBlockedHost(uri.Host)
                && IsRetailerHost(uri.Host, retailerDomains)
                && ScraperHelpers.IsProductDetailUrl(item.Url))
            .Select(CreateCandidate)
            .DistinctBy(item => item.ProductUrl, StringComparer.OrdinalIgnoreCase)
            .ToList();
        BindTopLevelImages(body.Images, results);
        await WriteCacheAsync(cacheKey, [.. results]);
        return results;
    }

    private static string[] GetRetailerDomains(string? market) => (string.IsNullOrWhiteSpace(market)
            ? MarketRetailers.Values.SelectMany(domains => domains)
            : MarketRetailers.GetValueOrDefault(market.Trim(), []))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static bool IsRetailerHost(string host, IEnumerable<string> retailerDomains) => retailerDomains.Any(domain =>
        string.Equals(host, domain, StringComparison.OrdinalIgnoreCase)
        || host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));

    private static string BuildCacheKey(string query, string? market, string? categoryName, int page)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{market?.Trim().ToUpperInvariant()}|{categoryName?.Trim().ToLowerInvariant()}|{query.Trim().ToLowerInvariant()}|{page}"));
        // v13: sharafdg search-landing slugs (no /product/ key) stopped being
        // candidates; a version bump so old cached bursts can't serve them.
        return $"product-discovery:websearch:cache:v13:{Convert.ToHexString(bytes)[..32].ToLowerInvariant()}";
    }

    private static ProductSearchResult CreateCandidate(TavilyResult item)
    {
        var content = item.Content ?? string.Empty;
        var (price, currency, hasPrice) = MineSnippetPrice(content);
        var urlMarket = Uri.TryCreate(item.Url, UriKind.Absolute, out var productUri)
            ? MiddleEastMarkets.FindByProductUrl(productUri)
            : null;

        if (productUri is not null && hasPrice && !SnippetCurrencyMatchesStore(productUri.Host, currency, urlMarket))
            (price, currency, hasPrice) = (0m, string.Empty, false);
        var marketInfo = urlMarket ?? MiddleEastMarkets.All.FirstOrDefault(candidate =>
            string.Equals(candidate.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase));
        var image = item.Images
            .Select((element, index) => (Raw: GetImageUrl(element) ?? string.Empty, Index: index))
            .Where(pair => !IsJunkImage(pair.Raw))
            .Select(pair => (Scored: AmazonImageQuality(pair.Raw), Pair: pair))
            .OrderByDescending(pair => pair.Scored)
            .ThenBy(pair => pair.Pair.Index)
            .Select(pair => NormalizeImageUrl(pair.Pair.Raw))
            .FirstOrDefault(candidate => candidate is not null) ?? string.Empty;
        return new ProductSearchResult
        {
            Name = string.IsNullOrWhiteSpace(item.Title) ? item.Url : item.Title,
            Description = content,
            ImageUrl = image,
            ProductUrl = item.Url,
            StoreName = Uri.TryCreate(item.Url, UriKind.Absolute, out var uri) ? uri.Host : string.Empty,
            MarketCode = marketInfo?.Code ?? string.Empty,
            Country = marketInfo?.Country ?? string.Empty,
            Currency = hasPrice ? currency : string.Empty,
            Price = hasPrice ? price : 0,
            IsVerified = false,
            DiscoveryProvider = ProviderName
        };
    }

    private static readonly IReadOnlyDictionary<string, string[]> LocalStoreCurrencies =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["amazon.eg"] = ["EGP"], ["jumia.com.eg"] = ["EGP"], ["btech.com"] = ["EGP"],
            ["2b.com.eg"] = ["EGP"], ["sigma-computer.com"] = ["EGP"], ["compume.com"] = ["EGP"],
            ["carrefouregypt.com"] = ["EGP"], ["dream2000.com.eg"] = ["EGP"], ["rayashop.com"] = ["EGP"],
            ["elarabygroup.com"] = ["EGP"], ["amazon.ae"] = ["AED"], ["amazon.sa"] = ["SAR"],
            ["jumia.com.sa"] = ["SAR"], ["jarir.com"] = ["SAR"], ["extra.com"] = ["SAR"],
            ["saco.sa"] = ["SAR"], ["sharafdg.com"] = ["AED"], ["jumbo.ae"] = ["AED"],
            ["emaxonline.com"] = ["AED"], ["virginmegastore.ae"] = ["AED"], ["xcite.com"] = ["KWD"]
        };

    private static bool SnippetCurrencyMatchesStore(string host, string currency, MarketInfo? urlMarket)
    {
        if (currency.Length == 0) return true;
        if (host.EndsWith("noon.com", StringComparison.OrdinalIgnoreCase))
        {
            return urlMarket is null
                || string.Equals(currency, urlMarket.CurrencyCode, StringComparison.OrdinalIgnoreCase);
        }
        foreach (var (domain, allowed) in LocalStoreCurrencies)
        {
            if (!host.Equals(domain, StringComparison.OrdinalIgnoreCase)
                && !host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase)) continue;
            return allowed.Contains(currency, StringComparer.OrdinalIgnoreCase);
        }
        return true;
    }

    private async Task<ProductSearchResult[]?> ReadCacheAsync(string key)
    {
        if (redis is not null)
        {
            try
            {
                var value = await redis.GetDatabase().StringGetAsync(key);
                if (value.HasValue) return JsonSerializer.Deserialize<ProductSearchResult[]>(value.ToString());
            }
            catch (RedisException) { }
        }

        if (_localCache.TryGetValue(key, out var cached))
        {
            if (cached.ExpiresAtUtc > DateTime.UtcNow) return cached.Results;
            _localCache.TryRemove(key, out _);
        }
        return null;
    }

    private async Task WriteCacheAsync(string key, ProductSearchResult[] results)
    {
        if (redis is not null)
        {
            try
            {
                var json = JsonSerializer.Serialize(results);
                await redis.GetDatabase().StringSetAsync(key, json, CacheTtl);
                return;
            }
            catch (RedisException) { }
        }
        _localCache[key] = (DateTime.UtcNow.Add(CacheTtl), results);
    }

    private async Task ReserveQuotaAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var month = new DateOnly(now.Year, now.Month, 1);
        var quota = options.Value.MonthlyQuota;
        if (quota <= 0) throw new ProviderQuotaExceededException(Name, month);

        if (redis is not null)
        {
            var key = new RedisKey($"product-discovery:{Name.ToLowerInvariant()}:{month:yyyyMM}");
            var expiresAt = new DateTimeOffset(new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(1));
            var used = (long)await redis.GetDatabase().ScriptEvaluateAsync(
                ReserveQuotaScript,
                [key],
                [quota, expiresAt.ToUnixTimeSeconds()]);
            cancellationToken.ThrowIfCancellationRequested();
            if (used == 0) throw new ProviderQuotaExceededException(Name, month);
            return;
        }

        await _quotaLock.WaitAsync(cancellationToken);
        try
        {
            if (month != _quotaMonth)
            {
                _quotaMonth = month;
                _monthCalls = 0;
            }
            if (_monthCalls >= quota) throw new ProviderQuotaExceededException(Name, month);
            _monthCalls++;
        }
        finally { _quotaLock.Release(); }
    }

    private static bool IsBlockedHost(string host)
        => host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(host, out var ip) && !IPAddress.IsLoopback(ip) && !IsGlobalAddress(ip);

    private static bool IsGlobalAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return !(bytes[0] == 0 || bytes[0] == 10 || bytes[0] == 127 || bytes[0] == 169 && bytes[1] == 254
                || bytes[0] == 172 && bytes[1] is >= 16 and <= 31 || bytes[0] == 192 && bytes[1] == 168 || bytes[0] >= 224);
        }
        var ipv6 = address.GetAddressBytes();
        return ipv6[0] is 0x20 or 0x30;
    }

    private static string? GetImageUrl(JsonElement image)
        => image.ValueKind switch
        {
            JsonValueKind.String => image.GetString(),
            JsonValueKind.Object when image.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String => url.GetString(),
            _ => null
        };

    private static void BindTopLevelImages(List<JsonElement> images, List<ProductSearchResult> candidates)
    {
        if (images.Count == 0) return;
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var image in images)
        {
            var (url, sourceTitle) = ReadImageEntry(image);
            if (url is null || sourceTitle is null) continue;
            var normalized = NormalizeImageUrl(url);
            if (normalized is null || !used.Add(normalized)) continue;
            ProductSearchResult? best = null;
            var bestScore = 0;
            var secondScore = 0;
            foreach (var candidate in candidates)
            {
                if (candidate.ImageUrl.Length != 0) continue;
                var score = CountTitleTokensMatched(sourceTitle, candidate.Name);
                if (score > bestScore)
                {
                    secondScore = bestScore;
                    bestScore = score;
                    best = candidate;
                }
                else if (score > secondScore) secondScore = score;
            }
            if (best is not null && bestScore >= 3 && bestScore >= secondScore + 2)
            {
                best.ImageUrl = normalized;
                used.Add(normalized);
            }
            else used.Remove(normalized);
        }
    }

    private static (string? Url, string? SourceTitle) ReadImageEntry(JsonElement image)
        => image.ValueKind switch
        {
            JsonValueKind.Object when image.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String
                && image.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
                => (url.GetString(), title.GetString()),
            _ => (null, null)
        };

    private static string? NormalizeImageUrl(string url)
    {
        if (IsJunkImage(url)) return null;
        var normalized = NormalizeAmazonImage(url) ?? url;
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var imageUri)
            || imageUri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(imageUri.UserInfo)
            || IsBlockedHost(imageUri.Host))
            return null;
        return normalized;
    }

    private static int CountTitleTokensMatched(string imageTitle, string candidateName)
    {
        var tokens = NormalizeNameTokens(imageTitle).ToHashSet(StringComparer.Ordinal);
        if (tokens.Count == 0) return 0;
        return NormalizeNameTokens(candidateName)
            .Where(token => !TitleFillerTokens.Contains(token))
            .Distinct()
            .Count(candidate => tokens.Contains(candidate));
    }

    private static readonly HashSet<string> TitleFillerTokens = new(StringComparer.Ordinal)
    { "buy", "online", "best", "price", "eg", "ae", "sa", "official", "warranty", "souq", "amazon", "noon", "sites", "distributors" };

    private static IEnumerable<string> NormalizeNameTokens(string value)
        => Regex.Matches(value.Normalize(NormalizationForm.FormKC), @"\p{L}\p{Nd}+|\p{Nd}+|\p{L}+")
            .Select(match => match.Value.ToLowerInvariant());

    private static bool IsJunkImage(string? url) => ScraperHelpers.IsJunkImageUrl(url);

    private static readonly string[] NonPriceContextMarkers =
    [
        "save", "saving", "was ", "interest-free", "installment", "instalment",
        "payment", "months", "per month", "monthly", "credit", "easy",
        "stars", "out of", "rating", "review",
        "damage", "damaged", "replace", "replaced", "you pay", "pay for",
        "وفر", "توفير", "خصم", "أقساط", "قسط", "شهرية", "شهر", "دفعة", "دفعات",
    ];

    private static (decimal Price, string Currency, bool HasPrice) MineSnippetPrice(string content)
    {
        const string amountPattern = @"(?<amount>[\d,.٬٫]*\d|\d)";
        const string currencyPattern = @"EGP|SAR|AED|USD|EUR|GBP|BHD|JOD|KWD|OMR|QAR|TRY";
        const string localCurrencyPattern = @"د\.إ|ر\.س|ج\.م|د\.ك|د\.ا|د\.ب|ر\.ع\.?|ر\.ق";
        foreach (Match match in Regex.Matches(content,
            $@"(?:(?<currency>{currencyPattern})\s*[:\-]?\s*{amountPattern}|{amountPattern}\s*(?<currency>{currencyPattern})|(?<localCurrency>{localCurrencyPattern})\s*{amountPattern}|{amountPattern}\s*(?<localCurrency>{localCurrencyPattern})|(?<symbol>[$€£₺])\s*{amountPattern})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (IsInsideNonPriceContext(content, match.Index, match.Length)) continue;
            if (!ParseSnippetAmount(match, out var price)) continue;
            return (price, ResolveSnippetCurrency(match), true);
        }
        return (0m, string.Empty, false);
    }

    private static bool IsInsideNonPriceContext(string content, int matchIndex, int matchLength)
    {
        var near = MakeWindow(content, matchIndex, matchLength, 12, 12);
        if (NonPriceContextMarkers.Any(marker => near.Contains(marker, StringComparison.Ordinal)))
            return true;
        var far = MakeWindow(content, matchIndex, matchLength, 24, 0);
        if (!NonPriceContextMarkers.Any(marker => far.Contains(marker, StringComparison.Ordinal)))
            return false;
        return !PriceAffirmationMarkers.Any(marker => near.Contains(marker, StringComparison.Ordinal));
    }

    private static string MakeWindow(string content, int startIndex, int length, int back, int forward)
    {
        var start = Math.Max(0, startIndex - back);
        var end = Math.Min(content.Length, startIndex + length + forward);
        return content[start..end].ToLowerInvariant();
    }

    private static readonly string[] PriceAffirmationMarkers =
    [
        "price", "cost", "سعر", "السعر", "بسعر",
    ];

    private static bool ParseSnippetAmount(Match match, out decimal price)
    {
        price = 0m;
        var rawAmount = match.Groups["amount"].Value
            .Replace("٬", string.Empty).Replace("٫", ".")
            .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
            .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
            .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
            .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9');
        var dotPosition = rawAmount.LastIndexOf('.');
        var commaPosition = rawAmount.LastIndexOf(',');
        if (dotPosition >= 0 && commaPosition >= 0)
        {
            if (dotPosition < commaPosition) return false;
            rawAmount = rawAmount.Replace(",", string.Empty);
        }
        else if (dotPosition >= 0 && rawAmount.Length - dotPosition - 1 > 3)
            return false;
        return decimal.TryParse(rawAmount.Replace(",", string.Empty),
            NumberStyles.Number, CultureInfo.InvariantCulture, out price) && price > 0;
    }

    private static string ResolveSnippetCurrency(Match match)
    {
        var currency = match.Groups["currency"].Value.ToUpperInvariant();
        if (currency.Length == 0 && match.Groups["localCurrency"].Success)
            currency = match.Groups["localCurrency"].Value switch
            {
                "د.إ" => "AED", "ر.س" => "SAR", "ج.م" => "EGP", "د.ك" => "KWD", "د.ا" => "JOD",
                "د.ب" => "BHD", "ر.ع." or "ر.ع" => "OMR", "ر.ق" => "QAR", _ => string.Empty
            };
        if (currency.Length == 0 && match.Groups["symbol"].Success)
            currency = match.Groups["symbol"].Value switch
            {
                "€" => "EUR", "£" => "GBP", "₺" => "TRY", _ => "USD"
            };
        return currency;
    }

    private static int AmazonImageQuality(string url)
    {
        var lowered = url.ToLowerInvariant();
        if (!lowered.Contains("/images/i/")) return 0;
        if (lowered.Contains("_sy300_sx300_") || lowered.Contains("_sx300_sy300_")) return 3;
        if (lowered.Contains("_us40") || lowered.Contains("_us50")) return 1;
        return 2;
    }

    private static string? NormalizeAmazonImage(string url)
    {
        var match = Regex.Match(url, @"^(https://[^/]+/images/I/[^._/]+)\._[^.]*_\.(jpe?g|png|gif|webp)$", RegexOptions.IgnoreCase);
        return match.Success ? $"{match.Groups[1].Value}.{match.Groups[2].Value}" : url;
    }

    private sealed class TavilyResponse
    {
        [JsonPropertyName("results")] public List<TavilyResult> Results { get; set; } = [];
        [JsonPropertyName("images")] public List<JsonElement> Images { get; set; } = [];
    }

    private sealed class TavilyResult
    {
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string? Content { get; set; }
        [JsonPropertyName("images")] public List<JsonElement> Images { get; set; } = [];
    }
}

public sealed class ProviderQuotaExceededException(string providerName, DateOnly quotaMonth)
    : Exception($"{providerName} search quota was exhausted for {quotaMonth:yyyy-MM}.");
