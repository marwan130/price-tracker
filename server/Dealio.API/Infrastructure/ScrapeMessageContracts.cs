namespace Dealio.API.Infrastructure;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record ScrapeJobPayload(
    Guid JobId,
    Guid? ListingId,
    string Url,
    decimal? ExpectedRecheckPrice = null,
    Guid? SearchSessionId = null,
    string? CandidateName = null,
    string? CandidateStoreName = null,
    string? MarketCode = null,
    string? Country = null,
    string? DiscoveryProvider = null,
    bool RequireTitleMatch = true,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool DeferVerification = false);
/// <summary>
/// The Python worker stamps envelope times with datetime.now(timezone.utc)
/// .isoformat(), which ends in +00:00 rather than Z; System.Text.Json feeds
/// offsets through RoundtripKind, so they deserialize with Kind=Local and
/// PostgreSQL timestamptz columns reject the write. Every envelope datetime
/// therefore normalizes to UTC on the wire boundary itself.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String
            && reader.GetString() is { } raw
            && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            return parsed.UtcDateTime;
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime());
}

public sealed class NullableUtcDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return new UtcDateTimeJsonConverter().Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteStringValue(value.Value.ToUniversalTime());
        else writer.WriteNullValue();
    }
}

public sealed record ScrapeEnvelope<T>(
    int SchemaVersion,
    Guid MessageId,
    Guid CorrelationId,
    [property: JsonConverter(typeof(UtcDateTimeJsonConverter))] DateTime OccurredAtUtc,
    T Payload);

public sealed record ScrapeResultPayload(
    Guid JobId,
    Guid? ListingId,
    string Outcome,
    ScrapeExtraction? Extraction,
    ScrapeFailure? Failure,
    [property: JsonConverter(typeof(NullableUtcDateTimeJsonConverter))] DateTime? WorkerStartedAtUtc = null,
    int? DurationMs = null,
    Guid? SearchSessionId = null);
public sealed record ScrapeExtraction(string? Title, string? Price, string? Currency, string? ImageUrl, string? Availability, string? ExtractionMethod);
public sealed record ScrapeFailure(string Code, bool Retryable);
public sealed record SearchCompletionPayload(int Total, int Verified, int Candidates, bool Partial, int FailedSources, bool HasMore = false);
