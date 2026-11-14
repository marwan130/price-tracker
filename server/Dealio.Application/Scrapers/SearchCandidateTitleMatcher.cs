namespace PriceTracker.Application.Scrapers;

using System.Text.RegularExpressions;

public static partial class SearchCandidateTitleMatcher
{
    private static readonly Regex SearchPageTitle = new(
        @"\b(search results?|results for|best price offers|products matching|items matching)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> StoreWords = new(StringComparer.Ordinal)
    {
        "noon", "amazon", "jumia", "namshi", "uae", "ksa", "saudi", "egypt", "qatar", "oman", "bahrain"
    };

    public static bool Matches(string extracted, string expected)
    {
        if (LooksLikeSearchPageTitle(extracted) || LooksLikeSearchPageTitle(expected)) return false;
        var actualTokens = Tokens(extracted);
        var expectedTokens = Tokens(expected);
        if (actualTokens.Count == 0 || expectedTokens.Count == 0) return false;

        if (expectedTokens.Any(token => IsModelOrSpecToken(token) && !actualTokens.Contains(token)))
            return false;

        var shared = expectedTokens.Count(actualTokens.Contains);
        return shared >= 2 && shared / (double)Math.Min(actualTokens.Count, expectedTokens.Count) >= 0.7;
    }

    public static bool IsRelevantToSearch(string title, string query)
    {
        if (LooksLikeSearchPageTitle(title)) return false;
        if (ContainsAttachmentChatter(title, query)) return false;
        var queryTokens = Tokens(query);
        var titleTokens = Tokens(title);
        if (queryTokens.Count == 0 || titleTokens.Count == 0) return false;
        if (queryTokens.Count == 1) return titleTokens.Contains(queryTokens.Single());
        return Matches(title, query);
    }

    public static bool LooksLikeSearchPageTitle(string title)
        => !string.IsNullOrWhiteSpace(title) && SearchPageTitle.IsMatch(title);

    private static HashSet<string> Tokens(string value)
    {
        var normalized = Regex.Replace(value.ToLowerInvariant(),
            @"(?<=\d)\s+(?=(?:gb|g|tb|mah|mp|hz|inch|in)\b)", string.Empty);
        return Regex.Matches(normalized, @"[a-z0-9]+")
            .Select(match => match.Value)
            .Where(token => token.Length > 1 && !StoreWords.Contains(token))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static bool IsModelOrSpecToken(string token)
        => token.Any(char.IsAsciiDigit) && token.Any(char.IsAsciiLetter);
    private static readonly Regex AccessoryChatter = new(
        @"\b(case|cover|sleeve|skin|shell|pouch|holster|bumper|keychain|lanyard|holder|stand|mount|armband|strap|film|refill|stylus|cable|adapter|charger|dock|applecare|warranty|insurance|plan|generic|replacement|replica|knockoff|fake|compatible with|for (?:apple |samsung |huawei |xiaomi |redmi |oppo |vivo |realme )?(?:airpods|iphone|ipod|ipad|macbook|imac|galaxy|watch|pixel|ps4|ps5|xbox|switch|nintendo))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private static bool ContainsAttachmentChatter(string title, string query)
    {
        try
        {
            var chatter = AccessoryChatter.Matches(title);
            if (chatter.Count == 0) return false;
            if (AccessoryChatter.IsMatch(query)) return false;
            if (chatter.Count == 1
                && chatter[0].Value.Equals("case", StringComparison.OrdinalIgnoreCase)
                && BundledCaseWordSuffix.IsMatch(title[..chatter[0].Index]))
                return false;
            return true;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static readonly Regex BundledCaseWordSuffix = new(
        @"\b(?:charg\w*|magsafe|carry\w*)\s$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));
}
