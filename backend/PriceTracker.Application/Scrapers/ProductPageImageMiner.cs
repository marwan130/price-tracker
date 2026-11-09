namespace PriceTracker.Application.Scrapers;

using System.Net;
using System.Text.RegularExpressions;

public static class ProductPageImageMiner
{
    // Attribute order in real markup is not fixed 
    private static readonly Regex MetaImagePattern = new(
        @"<meta\b[^>]*?\b(?:property|name)=""(?<key>og:image(?:_secure_url)?|twitter:image(?:_src)?)""[^>]*?\bcontent=""(?<url>[^""]+)"""
        + @"|<meta\b[^>]*?\bcontent=""(?<url2>[^""]+)""[^>]*?\b(?:property|name)=""(?<key2>og:image(?:_secure_url)?|twitter:image(?:_src)?)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ItempropImagePattern = new(
        @"<(?:img|meta|span|div)\b[^>]*?\bitemprop=""image""[^>]*?\b(?:src|content)=""(?<url>[^""]+)"""
        + @"|<(?:img|meta|span|div)\b[^>]*?\b(?:src|content)=""(?<url2>[^""]+)""[^>]*?\bitemprop=""image""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // JSON-LD "image" can be a bare string or the first entry of a string array
    // meta patterns run before it so a well-placed og:image always wins.
    private static readonly Regex JsonLdImagePattern = new(
        @"""image""\s*:\s*(?:\[\s*)?""(?<url>https?:[^""]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex[] PatternsInPriorityOrder = [MetaImagePattern, ItempropImagePattern, JsonLdImagePattern];

    public static string? Mine(string head, Uri pageUrl)
    {
        // Storefront chrome (payment graphics, site sprites) leaks into scans
        // before the real photo, so every source keeps matching until a clean candidate appears 
        foreach (var pattern in PatternsInPriorityOrder)
            foreach (Match match in pattern.Matches(head))
                if (Pick(HtmlGroup(match), pageUrl) is { } resolved)
                    return resolved;
        return null;
    }

    private static string HtmlGroup(Match match)
        => WebUtility.HtmlDecode(match.Groups["url"].Success ? match.Groups["url"].Value : match.Groups["url2"].Value);

    private static string? Pick(string? raw, Uri pageUrl)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)) return null;

        Uri candidate;
        // Absolute or protocol-relative URLs resolve directly
        if (raw.StartsWith("//", StringComparison.Ordinal))
            candidate = new Uri("https:" + raw);
        else if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute) && absolute is not null)
            candidate = absolute;
        else if (Uri.TryCreate(pageUrl, new Uri(raw, UriKind.RelativeOrAbsolute), out var joined) && joined is not null)
            candidate = joined;
        else
            return null;

        if (candidate.Scheme == Uri.UriSchemeHttp)
            candidate = new UriBuilder(candidate) { Scheme = Uri.UriSchemeHttps, Port = 443 }.Uri;
        if (candidate.Scheme != Uri.UriSchemeHttps) return null;

        return ScraperHelpers.IsJunkImageUrl(candidate.ToString()) ? null : candidate.ToString();
    }
}
