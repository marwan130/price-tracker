namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Markets;
using PriceTracker.Infrastructure.Persistence;

[ApiController]
[Route("v1/products/search")]
[Authorize]
[EnableRateLimiting("search")]
public class ProductSearchController : ControllerBase
{
    private readonly IProductSearchService _productSearchService;
    private readonly ILogger<ProductSearchController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IProductCandidateVerificationQueue _candidateQueue;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ProductSearchController(
        IProductSearchService productSearchService,
        ILogger<ProductSearchController> logger,
        ApplicationDbContext db,
        IProductCandidateVerificationQueue candidateQueue)
    {
        _productSearchService = productSearchService;
        _logger = logger;
        _db = db;
        _candidateQueue = candidateQueue;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] ProductFilterRequest filter, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filter.Query))
            return BadRequest(ApiResponse<IEnumerable<ProductSearchResult>>.Fail("VALIDATION_ERROR", "Query is required"));
        if (!IsSupportedMarket(filter.Market))
            return BadRequest(ApiResponse<IEnumerable<ProductSearchResult>>.Fail("UNSUPPORTED_MARKET", "Choose one of the available store markets."));
        if (!filter.HasValidPriceRange())
            return BadRequest(ApiResponse<IEnumerable<ProductSearchResult>>.Fail("VALIDATION_ERROR", "Minimum price cannot be greater than maximum price."));

        var results = await _productSearchService.SearchProductsAsync(filter, ct);
        return Ok(ApiResponse<IEnumerable<ProductSearchResult>>.Ok(results));
    }

    [HttpPost("url")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchByUrl([FromBody] SearchByUrlRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(ApiResponse<ProductSearchResult?>.Fail("VALIDATION_ERROR", "URL is required"));
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            return BadRequest(ApiResponse<ProductSearchResult?>.Fail("INVALID_URL", "A public HTTPS product URL is required."));

        var marketInfo = MiddleEastMarkets.FindByProductUrl(uri);
        var now = DateTime.UtcNow;
        var session = new ProductSearchSession
        {
            SessionId = Guid.NewGuid(),
            RequestPayload = JsonSerializer.Serialize(new { url = uri.AbsoluteUri }, JsonOptions),
            MarketCode = marketInfo?.Code,
            Status = "awaiting_verification",
            CompletionPayload = JsonSerializer.Serialize(new { total = 1, verified = 0, candidates = 1, partial = false, failedSources = 0 }, JsonOptions),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ExpiresAtUtc = now.AddHours(1)
        };
        _db.ProductSearchSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        var staged = await _candidateQueue.StageAsync(session.SessionId, new ProductSearchResult
        {
            ProductUrl = uri.AbsoluteUri,
            StoreName = uri.Host,
            MarketCode = marketInfo?.Code ?? string.Empty,
            Country = marketInfo?.Country ?? string.Empty,
            DiscoveryProvider = "URL import"
        }, ct, requireTitleMatch: false, deferVerification: true);
        if (!staged)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail("SCRAPE_QUEUE_UNAVAILABLE", "The product page could not be queued for safe extraction."));

        var deadline = DateTime.UtcNow.AddSeconds(25);
        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            var item = await _db.ProductSearchSessionEvents.AsNoTracking()
                .Where(x => x.SessionId == session.SessionId && (x.EventType == "candidate" || x.EventType == "sourceFailed"))
                .OrderBy(x => x.Sequence)
                .FirstOrDefaultAsync(ct);
            if (item is not null)
            {
                if (item.EventType == "candidate")
                {
                    var result = JsonSerializer.Deserialize<ProductSearchResult>(item.Payload, JsonOptions);
                    if (result is not null) return Ok(ApiResponse<ProductSearchResult>.Ok(result));
                }
                return NotFound(ApiResponse<ProductSearchResult?>.Fail("NOT_FOUND", "Product details could not be read from that page."));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
        }

        return Accepted(ApiResponse<object>.Ok(new
        {
            sessionId = session.SessionId,
            streamUrl = $"/v1/products/search/stream?sessionId={session.SessionId}"
        }));
    }

    [HttpGet("stream")]
    [AllowAnonymous]
    public async Task<IActionResult> Stream([FromQuery] ProductFilterRequest filter, [FromQuery] Guid? sessionId, [FromQuery] int? after, CancellationToken ct)
    {
        var lastEventId = Request.Headers["Last-Event-ID"].ToString();
        var cursor = ParseEventCursor(lastEventId);
        if (cursor is not null)
        {
            if (sessionId.HasValue && sessionId.Value != cursor.Value.SessionId)
                return BadRequest(ApiResponse<object>.Fail("INVALID_SEARCH_CURSOR", "Search cursor does not match the requested session."));
            sessionId = cursor.Value.SessionId;
            after = cursor.Value.Sequence;
        }

        ProductSearchSession session;
        if (sessionId is null)
        {
            if (string.IsNullOrWhiteSpace(filter.Query))
                return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "Query is required."));
            if (filter.Query.Trim().Length > 200)
                return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "Query must be 200 characters or fewer."));
            if (!filter.HasValidPriceRange())
                return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "Minimum price cannot be greater than maximum price."));
            if (!IsSupportedMarket(filter.Market))
                return BadRequest(ApiResponse<object>.Fail("UNSUPPORTED_MARKET", "Choose one of the available store markets."));

            session = new ProductSearchSession
            {
                SessionId = Guid.NewGuid(),
                RequestPayload = JsonSerializer.Serialize(filter, JsonOptions),
                Status = "queued",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
            };
            _db.ProductSearchSessions.Add(session);
            await _db.SaveChangesAsync(ct);
            after = 0;
        }
        else
        {
            var resumedSession = await _db.ProductSearchSessions.AsNoTracking().SingleOrDefaultAsync(x => x.SessionId == sessionId.Value, ct);
            if (resumedSession is null) return NotFound(ApiResponse<object>.Fail("SEARCH_SESSION_NOT_FOUND", "Search session was not found."));
            if (resumedSession.ExpiresAtUtc <= DateTime.UtcNow) return StatusCode(StatusCodes.Status410Gone, ApiResponse<object>.Fail("SEARCH_SESSION_EXPIRED", "Search session has expired."));
            session = resumedSession;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Append("X-Accel-Buffering", "no");
        await Response.StartAsync(ct);
        var sequence = Math.Max(0, after ?? 0);
        var lastHeartbeat = DateTime.UtcNow;
        await Response.WriteAsync(": connected\n\n", ct);
        await Response.Body.FlushAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            var events = await _db.ProductSearchSessionEvents.AsNoTracking()
                .Where(x => x.SessionId == session.SessionId && x.Sequence > sequence)
                .OrderBy(x => x.Sequence).Take(100).ToListAsync(ct);
            foreach (var item in events)
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(item.Payload, JsonOptions);
                var envelope = JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    sessionId = session.SessionId,
                    sequence = item.Sequence,
                    occurredAt = item.OccurredAtUtc,
                    type = item.EventType,
                    payload
                }, JsonOptions);
                await Response.WriteAsync($"id: {session.SessionId:N}:{item.Sequence}\nevent: {item.EventType}\ndata: {envelope}\n\n", ct);
                sequence = item.Sequence;
            }

            if (events.Count > 0)
            {
                await Response.Body.FlushAsync(ct);
                lastHeartbeat = DateTime.UtcNow;
            }

            var currentStatus = await _db.ProductSearchSessions.AsNoTracking()
                .Where(x => x.SessionId == session.SessionId).Select(x => x.Status).SingleOrDefaultAsync(ct);
            if (currentStatus is null || currentStatus is "completed" or "failed" or "expired" || session.ExpiresAtUtc <= DateTime.UtcNow)
            {
                var hasPending = await _db.ProductSearchSessionEvents.AsNoTracking()
                    .AnyAsync(x => x.SessionId == session.SessionId && x.Sequence > sequence, ct);
                if (!hasPending) break;
            }

            if (DateTime.UtcNow - lastHeartbeat > TimeSpan.FromSeconds(15))
            {
                await Response.WriteAsync(": keep-alive\n\n", ct);
                await Response.Body.FlushAsync(ct);
                lastHeartbeat = DateTime.UtcNow;
            }
            // Events flowing: poll promptly so streaming feels live; idle waits
            // stretch out to avoid hammering the events table while the session
            // is just waiting on providers or scrape verification.
            await Task.Delay(events.Count > 0 ? TimeSpan.FromMilliseconds(150) : TimeSpan.FromMilliseconds(500), ct);
        }
        return new EmptyResult();
    }

    private static bool IsSupportedMarket(string? market)
        => string.IsNullOrWhiteSpace(market)
            || MiddleEastMarkets.Find(market) is not null
            || MiddleEastMarkets.All.Any(item => string.Equals(item.Country, market.Trim(), StringComparison.OrdinalIgnoreCase));

    private static (Guid SessionId, int Sequence)? ParseEventCursor(string value)
    {
        var separator = value.LastIndexOf(':');
        if (separator <= 0 || !Guid.TryParse(value[..separator], out var id)
            || !int.TryParse(value[(separator + 1)..], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var sequence) || sequence < 0)
            return null;
        return (id, sequence);
    }
}
