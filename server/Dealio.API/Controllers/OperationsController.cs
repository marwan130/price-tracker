namespace PriceTracker.API.Controllers;

using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StackExchange.Redis;
using PriceTracker.API.Infrastructure;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using PriceTracker.Domain.Markets;
using PriceTracker.Infrastructure.Persistence;
using PriceTracker.Infrastructure.Search;

[ApiController]
[Route("v1/operations")]
[Authorize(Roles = "Admin")]
public sealed class OperationsController(
    ApplicationDbContext db,
    ApiRequestMetrics requestMetrics,
    IOptions<RabbitMqOptions> rabbitOptions,
    IOptions<TavilyDiscoveryOptions> discoveryOptions,
    IConnectionMultiplexer? redis = null) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var scrapeRows = await db.ScrapeLogs.AsNoTracking().Where(log => log.StartedAt >= since)
            .Select(log => new { log.Status, log.StartedAt, log.FinishedAt }).ToListAsync(ct);
        var notificationOutcomes = await db.Notifications.AsNoTracking().Where(item => item.SentAt >= since)
            .GroupBy(item => item.Status).Select(group => new { Status = group.Key.ToString(), Count = group.Count() }).ToListAsync(ct);
        var searchOutcomes = await db.ProductSearchSessions.AsNoTracking().Where(session => session.CreatedAtUtc >= since)
            .GroupBy(session => session.Status).Select(group => new { Status = group.Key, Count = group.Count() }).ToListAsync(ct);
        var pendingOutbox = await db.OutboxMessages.AsNoTracking().Where(message => message.PublishedAtUtc == null)
            .Select(message => new { message.Attempts, message.CreatedAtUtc }).ToListAsync(ct);
        var currentMonth = DateTime.UtcNow.ToString("yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
        long? providerCalls = null;
        if (redis is not null)
        {
            var value = await redis.GetDatabase().StringGetAsync($"product-discovery:websearch:{currentMonth}");
            if (value.HasValue && long.TryParse(value.ToString(), out var parsed)) providerCalls = parsed;
        }
        object? rabbitMetrics = null;
        if (rabbitOptions.Value.Enabled)
        {
            try { rabbitMetrics = await GetRabbitMetricsAsync(rabbitOptions.Value, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { rabbitMetrics = new { available = false, message = "Queue metrics are temporarily unavailable." }; }
        }
        return Ok(ApiResponse<object>.Ok(new
        {
            generatedAtUtc = DateTime.UtcNow,
            apiRequests = requestMetrics.Snapshot(),
            activeUsers = await db.Users.CountAsync(user => user.IsActive, ct),
            activeTrackings = await db.UserProductTrackings.CountAsync(tracking => tracking.IsActive, ct),
            verifiedOffers = await db.StoreProductListings.CountAsync(listing => listing.IsActive && !listing.IsExcluded, ct),
            activeStores = await db.Stores.CountAsync(store => store.IsActive, ct),
            unpublishedOutboxMessages = pendingOutbox.Count,
            outboxRetries = pendingOutbox.Count(message => message.Attempts > 0),
            oldestUnpublishedOutboxAgeSeconds = pendingOutbox.Count == 0 ? 0 : (int)Math.Max(0, (DateTime.UtcNow - pendingOutbox.Min(message => message.CreatedAtUtc)).TotalSeconds),
            scrapeJobsLast24Hours = scrapeRows.Count,
            scrapeOutcomesLast24Hours = scrapeRows.GroupBy(row => row.Status.ToString()).Select(group => new { status = group.Key, count = group.Count() }),
            averageScrapeDurationMs = scrapeRows.Where(row => row.FinishedAt.HasValue).Select(row => (double?)(row.FinishedAt!.Value - row.StartedAt).TotalMilliseconds).Average(),
            searchSessionsLast24Hours = searchOutcomes,
            notificationOutcomesLast24Hours = notificationOutcomes,
            providerQuota = new { provider = "WebSearch", used = providerCalls, limit = discoveryOptions.Value.MonthlyQuota, month = currentMonth },
            rabbitMq = rabbitMetrics
        }));
    }

    private static async Task<object> GetRabbitMetricsAsync(RabbitMqOptions options, CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host, Port = options.Port, UserName = options.User, Password = options.Password,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(2), ClientProvidedName = "price-tracker-operations"
        };
        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var queueNames = new[] { "scrape.jobs.v1", "scrape.results.api.v1", "scrape.jobs.dead.v1", "scrape.results.api.dead.v1", "scrape.jobs.retry.5000ms.v1", "scrape.jobs.retry.30000ms.v1", "scrape.jobs.retry.120000ms.v1" };
        var queues = new List<object>(queueNames.Length);
        foreach (var name in queueNames)
        {
            var queue = await channel.QueueDeclarePassiveAsync(name, ct);
            queues.Add(new { name, messages = queue.MessageCount, consumers = queue.ConsumerCount });
        }
        return new { available = true, queues };
    }

    [HttpGet("match-reviews")]
    public async Task<IActionResult> GetMatchReviews(CancellationToken ct)
    {
        var reviews = await db.ProductSearchSessionEvents.AsNoTracking()
            .Where(item => item.EventType == "matchReview")
            .Include(item => item.Session)
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(500)
            .ToListAsync(ct);
        if (reviews.Count == 0) return Ok(ApiResponse<object>.Ok(Array.Empty<object>()));

        var sessionIds = reviews.Select(item => item.SessionId).Distinct().ToArray();
        var resolutions = await db.ProductSearchSessionEvents.AsNoTracking()
            .Where(item => sessionIds.Contains(item.SessionId) && item.EventType == "matchReviewResolved")
            .Select(item => item.Payload)
            .ToListAsync(ct);
        var resolvedIds = resolutions.Select(payload => JsonSerializer.Deserialize<MatchReviewResolution>(payload, JsonOptions)?.ReviewEventId)
            .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        var candidates = reviews.Where(item => !resolvedIds.Contains(item.Id))
            .Select(item => new { Event = item, Payload = JsonSerializer.Deserialize<MatchReviewPayload>(item.Payload, JsonOptions) })
            .Where(item => item.Payload is not null)
            .ToList();
        var productIds = candidates.Select(item => item.Payload!.SourceProductId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var productNames = await db.Products.AsNoTracking().Where(product => productIds.Contains(product.ProductId))
            .ToDictionaryAsync(product => product.ProductId, product => product.Name, ct);

        var result = candidates.Select(item => new
        {
            reviewEventId = item.Event.Id,
            sessionId = item.Event.SessionId,
            item.Event.OccurredAtUtc,
            sourceProductId = item.Payload!.SourceProductId,
            sourceProductName = item.Payload.SourceProductId is { } productId && productNames.TryGetValue(productId, out var name) ? name : null,
            reason = item.Payload.Reason,
            candidate = item.Payload.Candidate
        });
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("match-reviews/{sessionId:guid}/{reviewEventId:long}/resolve")]
    public async Task<IActionResult> ResolveMatchReview(Guid sessionId, long reviewEventId,
        [FromBody] ResolveMatchReviewRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var session = await db.ProductSearchSessions
            .FromSqlInterpolated($"SELECT * FROM \"ProductSearchSessions\" WHERE \"SessionId\" = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        var reviewEvent = await db.ProductSearchSessionEvents.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == reviewEventId && item.SessionId == sessionId && item.EventType == "matchReview", ct);
        if (session is null || reviewEvent is null) return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Match review was not found."));

        var resolutions = await db.ProductSearchSessionEvents.AsNoTracking()
            .Where(item => item.SessionId == sessionId && item.EventType == "matchReviewResolved")
            .Select(item => item.Payload).ToListAsync(ct);
        if (resolutions.Any(payload => JsonSerializer.Deserialize<MatchReviewResolution>(payload, JsonOptions)?.ReviewEventId == reviewEventId))
            return Conflict(ApiResponse<object>.Fail("ALREADY_RESOLVED", "Match review has already been resolved."));

        var review = JsonSerializer.Deserialize<MatchReviewPayload>(reviewEvent.Payload, JsonOptions);
        if (review?.Candidate is not { IsVerified: true } candidate || candidate.Price <= 0
            || !Uri.TryCreate(candidate.ProductUrl, UriKind.Absolute, out var productUri)
            || productUri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(productUri.UserInfo))
            return Conflict(ApiResponse<object>.Fail("INVALID_CANDIDATE", "The saved candidate is not eligible for resolution."));

        if (request.Accept)
        {
            if (request.ProductId is not { } productId
                || !await db.Products.AnyAsync(item => item.ProductId == productId, ct))
                return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "Choose an existing product to accept a match."));

            var currency = candidate.Currency.ToUpperInvariant();
            var market = session.MarketCode?.ToUpperInvariant();
            var marketInfo = MiddleEastMarkets.Find(market);
            var country = marketInfo?.Code ?? candidate.Country;
            var expectedCurrency = marketInfo?.CurrencyCode ?? currency;
            if (!MiddleEastMarkets.SupportedCurrencyCodes.Contains(currency)
                || !string.IsNullOrWhiteSpace(market) && marketInfo is null
                || !string.Equals(currency, expectedCurrency, StringComparison.OrdinalIgnoreCase)
                || !await db.Currencies.AnyAsync(item => item.Code == currency, ct))
                return Conflict(ApiResponse<object>.Fail("CURRENCY_MISMATCH", "Candidate currency does not match the selected market."));

            var baseUrl = productUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            if (await db.StoreProductListings.AnyAsync(item => item.ProductUrl == candidate.ProductUrl, ct))
                return Conflict(ApiResponse<object>.Fail("URL_ALREADY_LINKED", "This product URL is already linked."));
            var store = await db.Stores.FirstOrDefaultAsync(item => item.BaseUrl.ToLower() == baseUrl.ToLower() && item.Country == country, ct);
            if (store is not null && !string.Equals(store.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
                return Conflict(ApiResponse<object>.Fail("STORE_CURRENCY_MISMATCH", "The store is configured with a different currency."));
            store ??= new Store
            {
                StoreId = Guid.NewGuid(), Name = candidate.StoreName, BaseUrl = baseUrl,
                Country = country, CurrencyCode = currency, IsActive = true
            };
            if (db.Entry(store).State == EntityState.Detached) db.Stores.Add(store);
            var now = DateTime.UtcNow;
            var listing = new StoreProductListing
            {
                ListingId = Guid.NewGuid(), ProductId = productId, StoreId = store.StoreId,
                ProductUrl = candidate.ProductUrl, IsActive = candidate.InStock != false, LastScrapedAt = now
            };
            db.StoreProductListings.Add(listing);
            db.PriceHistories.Add(new PriceHistory
            {
                ListingId = listing.ListingId, Price = candidate.Price, CurrencyCode = currency,
                RecordedAt = now, ScrapedAt = now
            });
        }

        var decision = new MatchReviewResolution(reviewEventId, request.Accept, request.Accept ? request.ProductId : null, DateTime.UtcNow);
        var lastSequence = await db.ProductSearchSessionEvents.Where(item => item.SessionId == sessionId)
            .MaxAsync(item => (int?)item.Sequence, ct) ?? 0;
        db.ProductSearchSessionEvents.Add(new ProductSearchSessionEvent
        {
            SessionId = sessionId, Sequence = lastSequence + 1, EventType = "matchReviewResolved",
            Payload = JsonSerializer.Serialize(decision, JsonOptions), OccurredAtUtc = decision.ResolvedAtUtc
        });
        session.UpdatedAtUtc = decision.ResolvedAtUtc;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { resolved = true, accepted = request.Accept }));
    }

    [HttpGet("store-health")]
    public async Task<IActionResult> GetStoreHealth([FromQuery] int windowDays = 7, CancellationToken ct = default)
    {
        windowDays = Math.Clamp(windowDays, 1, 90);
        var since = DateTime.UtcNow.AddDays(-windowDays);
        var logs = await db.ScrapeLogs.AsNoTracking()
            .Where(log => log.StartedAt >= since)
            .OrderByDescending(log => log.StartedAt)
            .Take(10_000)
            .Select(log => new { log.StoreId, log.Status, log.StartedAt, log.FinishedAt })
            .ToListAsync(ct);
        var aggregates = logs.GroupBy(log => log.StoreId).ToDictionary(group => group.Key, group => new
        {
            Checks = group.Count(),
            Succeeded = group.Count(log => log.Status == ScrapeStatus.Success),
            Failed = group.Count(log => log.Status == ScrapeStatus.Failed),
            Partial = group.Count(log => log.Status == ScrapeStatus.Partial),
            LastAttemptUtc = group.Max(log => log.FinishedAt ?? log.StartedAt),
            LastSuccessUtc = group.Where(log => log.Status == ScrapeStatus.Success).Select(log => log.FinishedAt).Max(),
            AverageDurationMs = group.Where(log => log.FinishedAt != null)
                .Select(log => (double?)(log.FinishedAt!.Value - log.StartedAt).TotalMilliseconds)
                .Average()
        });

        var stores = await db.Stores.AsNoTracking()
            .OrderBy(store => store.Name)
            .Select(store => new
            {
                store.StoreId, store.Name, store.Country, store.CurrencyCode, store.IsActive,
                store.ConsecutiveScrapeFailures, store.AutoSuspendedUntilUtc
            })
            .Take(500)
            .ToListAsync(ct);

        var result = stores.Select(store =>
        {
            aggregates.TryGetValue(store.StoreId, out var metrics);
            var failureRate = metrics is null || metrics.Checks == 0 ? (double?)null
                : (double)metrics.Failed / metrics.Checks;
            var state = store.AutoSuspendedUntilUtc > DateTime.UtcNow ? "suspended"
                : metrics is null || metrics.Checks == 0 ? "unknown"
                : failureRate >= 0.5 ? "unhealthy"
                : failureRate >= 0.2 ? "degraded"
                : metrics.LastSuccessUtc is null || metrics.LastSuccessUtc < DateTime.UtcNow.AddDays(-2) ? "stale"
                : "healthy";
            return new
            {
                store.StoreId,
                store.Name,
                store.Country,
                store.CurrencyCode,
                store.IsActive,
                store.ConsecutiveScrapeFailures,
                store.AutoSuspendedUntilUtc,
                State = state,
                WindowDays = windowDays,
                Checks = metrics?.Checks ?? 0,
                Succeeded = metrics?.Succeeded ?? 0,
                Failed = metrics?.Failed ?? 0,
                Partial = metrics?.Partial ?? 0,
                FailureRate = failureRate,
                metrics?.LastAttemptUtc,
                metrics?.LastSuccessUtc,
                AverageDurationMs = metrics?.AverageDurationMs
            };
        });

        return Ok(ApiResponse<object>.Ok(new { generatedAtUtc = DateTime.UtcNow, sinceUtc = since, stores = result }));
    }
}

public sealed record ResolveMatchReviewRequest(bool Accept, Guid? ProductId);
internal sealed record MatchReviewPayload(Guid? SourceProductId, ProductSearchResult Candidate, string Reason);
internal sealed record MatchReviewResolution(long ReviewEventId, bool Accepted, Guid? ProductId, DateTime ResolvedAtUtc);
