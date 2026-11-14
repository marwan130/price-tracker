namespace Dealio.Domain.Markets;

public sealed record MarketInfo(string Code, string Country, string CurrencyCode, string CurrencyName, string CurrencySymbol);

public static class MiddleEastMarkets
{
    public static IReadOnlyList<MarketInfo> All { get; } =
    [
        new("AE", "United Arab Emirates", "AED", "UAE dirham", "د.إ"),
        new("BH", "Bahrain", "BHD", "Bahraini dinar", ".د.ب"),
        new("EG", "Egypt", "EGP", "Egyptian pound", "ج.م"),
        new("JO", "Jordan", "JOD", "Jordanian dinar", "د.ا"),
        new("KW", "Kuwait", "KWD", "Kuwaiti dinar", "د.ك"),
        new("OM", "Oman", "OMR", "Omani rial", "ر.ع."),
        new("QA", "Qatar", "QAR", "Qatari riyal", "ر.ق"),
        new("SA", "Saudi Arabia", "SAR", "Saudi riyal", "ر.س"),
        new("TR", "Türkiye", "TRY", "Turkish lira", "₺")
    ];

    public static IReadOnlySet<string> SupportedCurrencyCodes { get; } =
        All.Select(market => market.CurrencyCode).Concat(["USD", "EUR", "GBP"]).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static MarketInfo? Find(string? code) => All.FirstOrDefault(m =>
        string.Equals(m.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static MarketInfo? FindByCountry(string? country) => All.FirstOrDefault(m =>
        string.Equals(m.Code, country?.Trim(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(m.Country, country?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static MarketInfo? FindByProductUrl(Uri uri)
    {
        var host = uri.IdnHost;
        if (IsHost(host, "jumia.com.eg") || IsHost(host, "amazon.eg") || IsHost(host, "2b.com.eg") || IsHost(host, "ubuy.com.eg")) return Find("EG");
        if (IsHost(host, "jumia.com.sa") || IsHost(host, "amazon.sa") || IsHost(host, "ubuy.com.sa")) return Find("SA");
        if (IsHost(host, "amazon.ae") || IsHost(host, "ubuy.ae")) return Find("AE");
        if (IsHost(host, "ubuy.com.kw")) return Find("KW");
        if (IsHost(host, "ubuy.com")) return Find("JO");

        if (!IsHost(host, "noon.com") && !IsHost(host, "namshi.com")) return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments.Take(2))
        {
            var tokens = segment.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var market = tokens.Select(token => token.ToLowerInvariant()).FirstOrDefault(token => token switch
            {
                "ae" or "uae" => true,
                "bh" or "bahrain" => true,
                "eg" or "egypt" => true,
                "jo" or "jordan" => true,
                "kw" or "kuwait" => true,
                "om" or "oman" => true,
                "qa" or "qatar" => true,
                "sa" or "ksa" or "saudi" => true,
                "tr" or "turkey" or "türkiye" => true,
                _ => false
            });
            if (market is null) continue;
            var code = market switch
            {
                "ae" or "uae" => "AE",
                "bh" or "bahrain" => "BH",
                "eg" or "egypt" => "EG",
                "jo" or "jordan" => "JO",
                "kw" or "kuwait" => "KW",
                "om" or "oman" => "OM",
                "qa" or "qatar" => "QA",
                "sa" or "ksa" or "saudi" => "SA",
                "tr" or "turkey" or "türkiye" => "TR",
                _ => string.Empty
            };
            return Find(code);
        }

        return null;
    }

    private static bool IsHost(string host, string domain)
        => host.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase);
}
