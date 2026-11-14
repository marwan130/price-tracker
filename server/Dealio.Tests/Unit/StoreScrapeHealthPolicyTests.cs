namespace PriceTracker.Tests.Unit;

using FluentAssertions;
using PriceTracker.API.Infrastructure;
using PriceTracker.Domain.Entities;

public sealed class StoreScrapeHealthPolicyTests
{
    [Fact]
    public void RecordResult_SuspendsAfterThresholdAndExtendsOnlyAfterCurrentSuspensionExpires()
    {
        var now = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        var store = new Store();

        for (var failure = 1; failure < StoreScrapeHealthPolicy.FailureThreshold; failure++)
            StoreScrapeHealthPolicy.RecordResult(store, succeeded: false, nowUtc: now).Should().BeFalse();

        StoreScrapeHealthPolicy.RecordResult(store, succeeded: false, nowUtc: now).Should().BeTrue();
        store.ConsecutiveScrapeFailures.Should().Be(StoreScrapeHealthPolicy.FailureThreshold);
        store.AutoSuspendedUntilUtc.Should().Be(now.Add(StoreScrapeHealthPolicy.SuspensionDuration));

        StoreScrapeHealthPolicy.RecordResult(store, succeeded: false, nowUtc: now.AddHours(1)).Should().BeFalse();
        store.AutoSuspendedUntilUtc.Should().Be(now.Add(StoreScrapeHealthPolicy.SuspensionDuration));

        StoreScrapeHealthPolicy.RecordResult(store, succeeded: false,
            nowUtc: now.Add(StoreScrapeHealthPolicy.SuspensionDuration)).Should().BeTrue();
    }

    [Fact]
    public void RecordResult_SuccessResetsFailureStreakAndAutomaticSuspension()
    {
        var store = new Store
        {
            ConsecutiveScrapeFailures = StoreScrapeHealthPolicy.FailureThreshold,
            AutoSuspendedUntilUtc = DateTime.UtcNow.AddHours(2)
        };

        StoreScrapeHealthPolicy.RecordResult(store, succeeded: true, nowUtc: DateTime.UtcNow).Should().BeFalse();

        store.ConsecutiveScrapeFailures.Should().Be(0);
        store.AutoSuspendedUntilUtc.Should().BeNull();
    }
}
