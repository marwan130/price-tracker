namespace Dealio.Tests.Unit;

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Dealio.Application.DTOs.Currencies;
using Dealio.Infrastructure.Currencies;

public sealed class ExchangeRateConversionServiceTests
{
    [Fact]
    public async Task ConvertAsync_ConvertsUsingFreshBaseRates_AndKeepsOriginalAmounts()
    {
        var updated = DateTimeOffset.UtcNow.AddHours(-1);
        var next = DateTimeOffset.UtcNow.AddHours(23);
        var body = JsonSerializer.Serialize(new
        {
            result = "success",
            base_code = "USD",
            time_last_update_unix = updated.ToUnixTimeSeconds(),
            time_next_update_unix = next.ToUnixTimeSeconds(),
            rates = new Dictionary<string, decimal> { ["USD"] = 1, ["EGP"] = 50, ["SAR"] = 3.75m, ["AED"] = 3.67m }
        });
        var requests = 0;
        var handler = new StubHandler(_ =>
        {
            Interlocked.Increment(ref requests);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        });
        var service = new ExchangeRateConversionService(new StubClientFactory(new HttpClient(handler)), new MemoryCache(new MemoryCacheOptions()), logger: NullLogger<ExchangeRateConversionService>.Instance);
        var request = new CurrencyConversionRequest
        {
            TargetCurrency = "SAR",
            Items =
            [
                new() { Amount = 500, CurrencyCode = "EGP" },
                new() { Amount = 10, CurrencyCode = "USD" },
                new() { Amount = 15, CurrencyCode = "CHF" }
            ]
        };

        var result = await service.ConvertAsync(request);
        var cached = await service.ConvertAsync(request);

        result.Items[0].ConvertedAmount.Should().Be(37.50m);
        result.Items[0].ConversionRate.Should().Be(0.075m);
        result.Items[0].Amount.Should().Be(500m);
        result.Items[0].CurrencyCode.Should().Be("EGP");
        result.Items[1].ConvertedAmount.Should().Be(37.50m);
        result.Items[1].ConversionRate.Should().Be(3.75m);
        result.Items[2].ConvertedAmount.Should().BeNull("unsupported source currencies must not be guessed");
        result.RateUpdatedAtUtc.Should().BeCloseTo(updated.UtcDateTime, TimeSpan.FromSeconds(1));
        cached.Items[0].ConvertedAmount.Should().Be(37.50m);
        requests.Should().Be(1, "the shared daily rate snapshot is cached between conversions");
    }

    [Fact]
    public async Task ConvertAsync_RejectsExpiredRateSnapshots()
    {
        var updated = DateTimeOffset.UtcNow.AddHours(-40);
        var next = DateTimeOffset.UtcNow.AddHours(-16);
        var body = JsonSerializer.Serialize(new
        {
            result = "success",
            base_code = "USD",
            time_last_update_unix = updated.ToUnixTimeSeconds(),
            time_next_update_unix = next.ToUnixTimeSeconds(),
            rates = new Dictionary<string, decimal> { ["USD"] = 1, ["EGP"] = 50, ["SAR"] = 3.75m }
        });
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
        var service = new ExchangeRateConversionService(new StubClientFactory(new HttpClient(handler)), new MemoryCache(new MemoryCacheOptions()), logger: NullLogger<ExchangeRateConversionService>.Instance);

        var act = () => service.ConvertAsync(new CurrencyConversionRequest
        {
            TargetCurrency = "SAR",
            Items = [new() { Amount = 500, CurrencyCode = "EGP" }]
        });

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ConvertAsync_UsesConfiguredIntradayProviderAndCachesItsSnapshot()
    {
        var updated = DateTimeOffset.UtcNow.AddSeconds(-20);
        var body = JsonSerializer.Serialize(new
        {
            meta = new { base_currency = "USD", last_updated_at = updated },
            data = new Dictionary<string, decimal> { ["USD"] = 1, ["EGP"] = 50, ["SAR"] = 3.75m }
        });
        var requests = 0;
        var handler = new StubHandler(request =>
        {
            Interlocked.Increment(ref requests);
            request.RequestUri!.Host.Should().Be("api.currencyapi.com");
            request.Headers.GetValues("apikey").Single().Should().Be("test-currency-key");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        });
        var service = new ExchangeRateConversionService(
            new StubClientFactory(new HttpClient(handler)),
            new MemoryCache(new MemoryCacheOptions()),
            logger: NullLogger<ExchangeRateConversionService>.Instance,
            options: Options.Create(new CurrencyRateOptions { ApiKey = "test-currency-key", RefreshMinutes = 1 }));
        var request = new CurrencyConversionRequest
        {
            TargetCurrency = "SAR",
            Items = [new() { Amount = 500, CurrencyCode = "EGP" }]
        };

        var first = await service.ConvertAsync(request);
        var second = await service.ConvertAsync(request);

        first.Provider.Should().Be("CurrencyAPI");
        first.Items[0].ConvertedAmount.Should().Be(37.50m);
        second.RateUpdatedAtUtc.Should().BeCloseTo(updated.UtcDateTime, TimeSpan.FromSeconds(1));
        requests.Should().Be(1);
    }

    private sealed class StubClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
