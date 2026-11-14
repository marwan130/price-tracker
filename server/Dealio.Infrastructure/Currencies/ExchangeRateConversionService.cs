namespace Dealio.Infrastructure.Currencies;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dealio.Application.DTOs.Currencies;
using Dealio.Application.Interfaces.Services;

public sealed class ExchangeRateConversionService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache localCache,
    ICacheService? cache = null,
    ILogger<ExchangeRateConversionService>? logger = null,
    IOptions<CurrencyRateOptions>? options = null) : ICurrencyConversionService
{
    private const string OpenProviderName = "ExchangeRate-API Open Access";
    private const string OpenProviderUrl = "https://open.er-api.com/v6/latest/USD";
    private const string OpenAttributionUrl = "https://www.exchangerate-api.com";
    private const string CurrencyApiUrl = "https://api.currencyapi.com/v3/latest?base_currency=USD&shape=compact";
    private static readonly TimeSpan MaximumRateAge = TimeSpan.FromHours(36);
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private const string OpenCacheKey = "fx:open-er-api:usd";
    private const string CurrencyApiCacheKey = "fx:currencyapi:usd";
    private readonly CurrencyRateOptions _options = options?.Value ?? new CurrencyRateOptions();
    private bool UsesCurrencyApi => !string.IsNullOrWhiteSpace(_options.ApiKey);
    private string ProviderName => UsesCurrencyApi ? "CurrencyAPI" : OpenProviderName;
    private string AttributionUrl => UsesCurrencyApi ? "https://currencyapi.com" : OpenAttributionUrl;
    private string CacheKey => UsesCurrencyApi ? CurrencyApiCacheKey : OpenCacheKey;

    public async Task<CurrencyConversionResponse> ConvertAsync(CurrencyConversionRequest request, CancellationToken cancellationToken = default)
    {
        var target = NormalizeCode(request.TargetCurrency);
        var items = request.Items ?? [];
        var needsRates = items.Any(item => NormalizeCode(item.CurrencyCode) != target);
        RateSnapshot? snapshot = needsRates ? await GetRatesAsync(cancellationToken) : null;

        var converted = items.Select(item =>
        {
            var source = NormalizeCode(item.CurrencyCode);
            decimal? result = null;
            decimal? conversionRate = null;
            if (source == target)
            {
                result = item.Amount;
                conversionRate = 1m;
            }
            else if (snapshot is not null
                && TryGetRate(snapshot.Rates, source, out var sourceRate)
                && TryGetRate(snapshot.Rates, target, out var targetRate))
            {
                conversionRate = targetRate / sourceRate;
                result = Math.Round(item.Amount * targetRate / sourceRate, 2, MidpointRounding.AwayFromZero);
            }

            return new CurrencyConversionResult
            {
                Amount = item.Amount,
                CurrencyCode = source,
                ConvertedAmount = result,
                ConversionRate = conversionRate
            };
        }).ToArray();

        return new CurrencyConversionResponse
        {
            TargetCurrency = target,
            RateUpdatedAtUtc = snapshot?.UpdatedAtUtc,
            Provider = ProviderName,
            AttributionUrl = AttributionUrl,
            Items = converted
        };
    }

    public async Task<CurrencyRatesResponse> GetRateSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetRatesAsync(cancellationToken);
        return new CurrencyRatesResponse
        {
            Base = "USD",
            RateUpdatedAtUtc = snapshot.UpdatedAtUtc,
            NextUpdateAtUtc = snapshot.NextUpdateAtUtc,
            Provider = ProviderName,
            AttributionUrl = AttributionUrl,
            Rates = snapshot.Rates
        };
    }

    private async Task<RateSnapshot> GetRatesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cached = await GetCachedSnapshotAsync(cancellationToken);
        if (IsFresh(cached, now)) return cached!;

        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            now = DateTime.UtcNow;
            cached = await GetCachedSnapshotAsync(cancellationToken);
            if (IsFresh(cached, now)) return cached!;

            using var request = new HttpRequestMessage(HttpMethod.Get, UsesCurrencyApi ? CurrencyApiUrl : OpenProviderUrl);
            if (UsesCurrencyApi) request.Headers.TryAddWithoutValidation("apikey", _options.ApiKey);
            using var response = await httpClientFactory.CreateClient("fx-rates").SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            RateSnapshot snapshot;
            if (UsesCurrencyApi)
            {
                var payload = await response.Content.ReadFromJsonAsync<CurrencyApiResponse>(cancellationToken: cancellationToken)
                    ?? throw new InvalidDataException("Currency provider returned an empty response.");
                var meta = payload.Meta;
                if (meta is null || !string.Equals(meta.BaseCurrency, "USD", StringComparison.OrdinalIgnoreCase)
                    || meta.LastUpdatedAt is null || payload.Data is null)
                    throw new InvalidDataException("Currency provider returned an invalid rate snapshot.");
                snapshot = new RateSnapshot
                {
                    UpdatedAtUtc = meta.LastUpdatedAt.Value.UtcDateTime,
                    NextUpdateAtUtc = meta.LastUpdatedAt.Value.UtcDateTime.Add(MaximumRateAge),
                    Rates = payload.Data.ToDictionary(pair => NormalizeCode(pair.Key), pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                };
            }
            else
            {
                var payload = await response.Content.ReadFromJsonAsync<OpenExchangeRateResponse>(cancellationToken: cancellationToken)
                    ?? throw new InvalidDataException("Exchange-rate provider returned an empty response.");
                if (!string.Equals(payload.Result, "success", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(payload.BaseCode, "USD", StringComparison.OrdinalIgnoreCase)
                    || payload.Rates is null || payload.TimeLastUpdateUnix <= 0 || payload.TimeNextUpdateUnix <= 0)
                    throw new InvalidDataException("Exchange-rate provider returned an invalid rate snapshot.");
                snapshot = new RateSnapshot
                {
                    UpdatedAtUtc = DateTimeOffset.FromUnixTimeSeconds(payload.TimeLastUpdateUnix).UtcDateTime,
                    NextUpdateAtUtc = DateTimeOffset.FromUnixTimeSeconds(payload.TimeNextUpdateUnix).UtcDateTime,
                    Rates = payload.Rates.ToDictionary(pair => NormalizeCode(pair.Key), pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                };
            }
            if (!IsFresh(snapshot, DateTime.UtcNow))
                throw new InvalidDataException("Exchange-rate provider returned rates outside the accepted freshness window.");

            var cacheDuration = UsesCurrencyApi
                ? TimeSpan.FromMinutes(Math.Clamp(_options.RefreshMinutes, 1, 1440))
                : MaximumRateAge;
            localCache.Set(CacheKey, snapshot, cacheDuration);
            if (cache is not null)
                await cache.SetAsync(CacheKey, snapshot, cacheDuration, cancellationToken);
            return snapshot;
        }
        catch (HttpRequestException ex)
        {
            logger?.LogWarning(ex, "Exchange-rate provider is unavailable; currency conversion is omitted.");
            throw;
        }
        finally { RefreshLock.Release(); }
    }

    private async Task<RateSnapshot?> GetCachedSnapshotAsync(CancellationToken cancellationToken)
    {
        var cached = cache is null ? null : await cache.GetAsync<RateSnapshot>(CacheKey, cancellationToken);
        if (IsFresh(cached, DateTime.UtcNow)) return cached;
        return localCache.TryGetValue(CacheKey, out RateSnapshot? local) && IsFresh(local, DateTime.UtcNow) ? local : cached;
    }

    private static bool IsFresh(RateSnapshot? snapshot, DateTime now)
        => snapshot is not null
            && snapshot.UpdatedAtUtc <= now.AddMinutes(5)
            && snapshot.UpdatedAtUtc >= now.Subtract(MaximumRateAge)
            && snapshot.NextUpdateAtUtc > now;

    private static bool TryGetRate(IReadOnlyDictionary<string, decimal> rates, string code, out decimal rate)
    {
        if (code == "USD")
        {
            rate = 1m;
            return true;
        }
        return rates.TryGetValue(code, out rate) && rate > 0;
    }

    private static string NormalizeCode(string? code) => code?.Trim().ToUpperInvariant() ?? string.Empty;

    private sealed class OpenExchangeRateResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;
        [JsonPropertyName("base_code")]
        public string BaseCode { get; set; } = string.Empty;
        [JsonPropertyName("time_last_update_unix")]
        public long TimeLastUpdateUnix { get; set; }
        [JsonPropertyName("time_next_update_unix")]
        public long TimeNextUpdateUnix { get; set; }
        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }

    private sealed class CurrencyApiResponse
    {
        [JsonPropertyName("meta")] public CurrencyApiMeta? Meta { get; set; }
        [JsonPropertyName("data")] public Dictionary<string, decimal>? Data { get; set; }
    }

    private sealed class CurrencyApiMeta
    {
        [JsonPropertyName("base_currency")] public string BaseCurrency { get; set; } = string.Empty;
        [JsonPropertyName("last_updated_at")] public DateTimeOffset? LastUpdatedAt { get; set; }
    }

    private sealed class RateSnapshot
    {
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime NextUpdateAtUtc { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class CurrencyRateOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public int RefreshMinutes { get; set; } = 1440;
}
