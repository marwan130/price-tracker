namespace PriceTracker.Tests.Unit;

using System.Text.Json;
using FluentAssertions;
using PriceTracker.API.Infrastructure;

public sealed class ApiRequestMetricsTests
{
    [Fact]
    public void SnapshotReportsCountAverageAndP95ForTheRollingWindow()
    {
        var metrics = new ApiRequestMetrics();
        metrics.Record(200, 10);
        metrics.Record(500, 20);
        metrics.Record(200, 30);

        using var snapshot = JsonDocument.Parse(JsonSerializer.Serialize(metrics.Snapshot()));
        var data = snapshot.RootElement;
        data.GetProperty("requestCount").GetInt32().Should().Be(3);
        data.GetProperty("averageLatencyMs").GetDouble().Should().Be(20);
        data.GetProperty("p95LatencyMs").GetDouble().Should().Be(30);
    }
}
