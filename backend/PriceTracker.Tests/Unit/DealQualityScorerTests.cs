namespace PriceTracker.Tests.Unit;

using FluentAssertions;
using PriceTracker.Application.DTOs.Listings;
using PriceTracker.Application.Services;
using PriceTracker.Domain.Entities;

public sealed class DealQualityScorerTests
{
    [Fact]
    public void Assess_ReportsInsufficientHistoryInsteadOfInventingAScore()
    {
        var listing = ListingWithObservations(3, 10);

        var assessment = DealQualityScorer.Assess(listing);

        assessment.Score.Should().BeNull();
        assessment.HasSufficientHistory.Should().BeFalse();
        assessment.Summary.Should().Contain("Insufficient");
    }

    [Fact]
    public void Assess_ExplainsARealPriceDropAgainstStableHistory()
    {
        var listing = ListingWithObservations(6, 10);
        listing.PriceHistories.Add(new PriceHistory
        {
            Id = 7, ListingId = listing.ListingId, Price = 70m, CurrencyCode = "EGP",
            RecordedAt = DateTime.UtcNow, ScrapedAt = DateTime.UtcNow
        });

        var assessment = DealQualityScorer.Assess(listing);

        assessment.Score.Should().BeInRange(75, 100);
        assessment.AlgorithmVersion.Should().Be(DealQualityAssessment.CurrentAlgorithmVersion);
        assessment.HasSufficientHistory.Should().BeTrue();
        assessment.HistoricalMedian.Should().Be(100m);
        assessment.HistoricalLow.Should().Be(100m);
        assessment.Factors.Should().Contain(factor => factor.Contains("median", StringComparison.OrdinalIgnoreCase));
        assessment.Summary.Should().Contain("does not verify store authenticity");
    }

    [Fact]
    public void Assess_UsesOnlySameCurrencyObservations()
    {
        var listing = ListingWithObservations(6, 10);
        listing.PriceHistories.Add(new PriceHistory
        {
            Id = 99, ListingId = listing.ListingId, Price = 1m, CurrencyCode = "USD",
            RecordedAt = DateTime.UtcNow.AddDays(-15), ScrapedAt = DateTime.UtcNow.AddDays(-15)
        });

        var assessment = DealQualityScorer.Assess(listing);

        assessment.HistoricalMedian.Should().Be(100m);
    }

    private static StoreProductListing ListingWithObservations(int count, int dayStep)
    {
        var now = DateTime.UtcNow;
        var listing = new StoreProductListing { ListingId = Guid.NewGuid(), IsActive = true };
        for (var index = 0; index < count; index++)
        {
            var at = now.AddDays(-(double)dayStep * (count - index));
            listing.PriceHistories.Add(new PriceHistory
            {
                Id = index + 1,
                ListingId = listing.ListingId,
                Price = 100m,
                CurrencyCode = "EGP",
                RecordedAt = at,
                ScrapedAt = at
            });
        }
        return listing;
    }
}
