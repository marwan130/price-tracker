namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Listings;
using PriceTracker.Domain.Entities;

/// <summary>Scores historical prices; it does not verify retailers or guarantee a discount.</summary>
public static class DealQualityScorer
{
    private const int MinimumObservations = 5;
    private static readonly TimeSpan MinimumCoverage = TimeSpan.FromDays(7);
    private static readonly TimeSpan Lookback = TimeSpan.FromDays(180);

    public static DealQualityAssessment Assess(StoreProductListing listing)
    {
        var current = listing.PriceHistories
            .Where(x => x.Price > 0 && IsCurrencyCode(x.CurrencyCode))
            .OrderBy(x => x.RecordedAt).ThenBy(x => x.Id)
            .LastOrDefault();
        if (current is null)
            return Insufficient(0, null, null, null, "No valid price observations are available.");

        var observations = listing.PriceHistories
            .Where(x => x.Price > 0
                && string.Equals(x.CurrencyCode, current.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                && x.RecordedAt >= current.RecordedAt.Subtract(Lookback)
                && x.RecordedAt <= current.RecordedAt)
            .OrderBy(x => x.RecordedAt).ThenBy(x => x.Id)
            .TakeLast(61)
            .ToArray();
        var baseline = observations.Take(Math.Max(0, observations.Length - 1)).ToArray();
        var span = observations.Length == 0 ? TimeSpan.Zero : observations[^1].RecordedAt - observations[0].RecordedAt;
        if (baseline.Length < MinimumObservations || span < MinimumCoverage)
            return Insufficient(observations.Length, observations.FirstOrDefault()?.RecordedAt, null, null,
                "Insufficient historical coverage to assess this offer reliably.");

        var baselinePrices = baseline.Select(x => x.Price).OrderBy(x => x).ToArray();
        var median = Median(baselinePrices);
        var low = baselinePrices[0];
        var currentPrice = current.Price;
        var discount = median <= 0 ? 0m : (decimal)Math.Clamp(((double)median - (double)currentPrice) / (double)median, 0d, 1d);
        var lowRange = median - low;
        var proximity = lowRange <= 0 ? (currentPrice <= low ? 1m : 0m)
            : (decimal)Math.Clamp(((double)median - (double)currentPrice) / (double)lowRange, 0d, 1d);
        var mean = baselinePrices.Average(x => (double)x);
        var standardDeviation = Math.Sqrt(baselinePrices.Average(x => Math.Pow((double)x - mean, 2)));
        var coefficientOfVariation = standardDeviation / mean;
        var stability = (decimal)Math.Clamp(1d - coefficientOfVariation / 0.25d, 0d, 1d);
        var countCoverage = Math.Clamp(baseline.Length / 20m, 0m, 1m);
        var timeCoverage = Math.Clamp((decimal)span.TotalDays / 60m, 0m, 1m);
        var coverage = Math.Min(countCoverage, timeCoverage);
        var age = DateTime.UtcNow - current.ScrapedAt;
        if (age < TimeSpan.Zero) age = TimeSpan.Zero;
        var freshness = age <= TimeSpan.FromHours(24) ? 1m
            : age <= TimeSpan.FromDays(3) ? 0.75m
            : age <= TimeSpan.FromDays(7) ? 0.5m
            : 0m;

        var score = (int)Math.Round(100m * (
            Math.Min(discount / 0.30m, 1m) * 0.45m
            + proximity * 0.25m
            + stability * 0.15m
            + coverage * 0.10m
            + freshness * 0.05m), MidpointRounding.AwayFromZero);
        var percentBelowMedian = Math.Max(0d, ((double)median - (double)currentPrice) / (double)median * 100d);
        var factors = new List<string>
        {
            $"Current price is {percentBelowMedian:0.#}% below the historical median.",
            currentPrice <= low ? "Current price matches or beats the observed historical low."
                : $"Historical low is {((double)currentPrice / (double)low - 1d) * 100d:0.#}% below the current price.",
            $"Historical price stability contributes {stability:P0} confidence.",
            $"Coverage uses {baseline.Length} prior observations across {span.TotalDays:0} days.",
            freshness > 0 ? "The listing was checked recently." : "The listing check is stale; confidence is reduced."
        };

        return new DealQualityAssessment
        {
            Score = Math.Clamp(score, 0, 100),
            HasSufficientHistory = true,
            ObservationCount = observations.Length,
            HistoryFromUtc = observations[0].RecordedAt,
            HistoricalMedian = decimal.Round(median, 2),
            HistoricalLow = low,
            Summary = "Historical price signal only; it does not verify store authenticity or guarantee a deal.",
            Factors = factors
        };
    }

    private static DealQualityAssessment Insufficient(int count, DateTime? from, decimal? median, decimal? low, string summary)
        => new()
        {
            HasSufficientHistory = false,
            ObservationCount = count,
            HistoryFromUtc = from,
            HistoricalMedian = median,
            HistoricalLow = low,
            Summary = summary
        };

    private static decimal Median(decimal[] sorted)
        => sorted.Length % 2 == 0
            ? sorted[sorted.Length / 2 - 1] + (sorted[sorted.Length / 2] - sorted[sorted.Length / 2 - 1]) / 2m
            : sorted[sorted.Length / 2];

    private static bool IsCurrencyCode(string? value)
        => value is { Length: 3 } && value.All(char.IsAsciiLetter);
}
