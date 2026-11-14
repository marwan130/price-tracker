namespace Dealio.Application.DTOs.Listings;

public sealed class DealQualityAssessment
{
    public const string CurrentAlgorithmVersion = "dealio-deal-quality-v1";

    public string AlgorithmVersion { get; init; } = CurrentAlgorithmVersion;
    public int? Score { get; init; }
    public bool HasSufficientHistory { get; init; }
    public int ObservationCount { get; init; }
    public DateTime? HistoryFromUtc { get; init; }
    public decimal? HistoricalMedian { get; init; }
    public decimal? HistoricalLow { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Factors { get; init; } = [];
}
