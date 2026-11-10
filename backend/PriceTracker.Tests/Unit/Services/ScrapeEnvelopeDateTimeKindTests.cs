namespace PriceTracker.Tests.Unit.Services;

using System.Text.Json;
using FluentAssertions;
using PriceTracker.API.Infrastructure;

public sealed class ScrapeEnvelopeDateTimeKindTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static ScrapeEnvelope<object?> Deserialize(string occurredAtUtc) => JsonSerializer.Deserialize<ScrapeEnvelope<object?>>(
        $$"""
        {"schemaVersion":1,"messageId":"{{Guid.NewGuid()}}","correlationId":"{{Guid.NewGuid()}}","occurredAtUtc":"{{occurredAtUtc}}","payload":null}
        """, JsonOptions)!;

    [Fact]
    public void OccurredAtUtc_EndingInZ_IsUtcKind()
    {
        var envelope = Deserialize("2026-11-07T02:56:07.754174Z");
        envelope.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        envelope.OccurredAtUtc.ToUniversalTime().Should().Be(new DateTime(2026, 11, 7, 2, 56, 7, 754, DateTimeKind.Utc).AddMicroseconds(174));
    }

    [Fact]
    public void OccurredAtUtc_WithIsoOffset_IsUtcKind()
    {
        // The Python worker stamps results with datetime.now(timezone.utc)
        // .isoformat() which ends in +00:00, never Z. System.Text.Json feeds
        // offsets through RoundtripKind, so without a normalizer this would
        // come back Kind=Local and Npgsql rejects Local for timestamptz.
        var envelope = Deserialize("2026-11-07T02:56:07.754174+00:00");
        envelope.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        envelope.OccurredAtUtc.Should().Be(new DateTime(2026, 11, 7, 2, 56, 7, 754, DateTimeKind.Utc).AddMicroseconds(174));
    }

    [Fact]
    public void WorkerStartedAtUtc_WithIsoOffset_IsUtcKind()
    {
        var payload = JsonSerializer.Deserialize<ScrapeResultPayload>(
            """{"jobId":"11111111-1111-4111-8111-111111111111","listingId":null,"outcome":"completed","workerStartedAtUtc":"2026-11-07T02:56:07.754174+00:00"}""", JsonOptions)!;
        payload.WorkerStartedAtUtc.Should().NotBeNull();
        payload.WorkerStartedAtUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }
}
