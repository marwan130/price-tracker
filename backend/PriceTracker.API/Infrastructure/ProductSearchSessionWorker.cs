namespace PriceTracker.API.Infrastructure;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Infrastructure.Persistence;

public sealed class ProductSearchSessionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProductSearchSessionWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var recovered = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!recovered)
                {
                    await RecoverInterruptedSessionsAsync(stoppingToken);
                    recovered = true;
                }

                var session = await ClaimNextAsync(stoppingToken);
                if (session is null)
                {
                    await CleanupExpiredAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                    continue;
                }

                await ProcessAsync(session, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Search-session worker iteration failed");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task<ProductSearchSession?> ClaimNextAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var claimed = await db.ProductSearchSessions
            .FromSqlInterpolated($"SELECT * FROM \"ProductSearchSessions\" WHERE \"Status\" = 'queued' AND \"ExpiresAtUtc\" > {now} AND (\"NextAttemptAtUtc\" IS NULL OR \"NextAttemptAtUtc\" <= {now}) ORDER BY \"CreatedAtUtc\" LIMIT 1 FOR UPDATE SKIP LOCKED")
            .ToListAsync(ct);
        var session = claimed.SingleOrDefault();
        if (session is null) return null;

        session.Status = "running";
        session.Attempts++;
        session.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return session;
    }

    private async Task ProcessAsync(ProductSearchSession claimed, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IProductSearchService>();
        var filter = JsonSerializer.Deserialize<ProductFilterRequest>(claimed.RequestPayload, JsonOptions)
            ?? throw new InvalidDataException("Stored search request is invalid.");
        using var appendLock = new SemaphoreSlim(1, 1);
        var candidateCount = 0;
        var failures = 0;

        try
        {
            var verificationQueue = scope.ServiceProvider.GetRequiredService<IProductCandidateVerificationQueue>();
            await AppendAsync(claimed.SessionId, "progress", new { stage = "discovering", message = "Searching available discovery sources." }, ct);
            var appendBuffer = new List<(string Type, object Payload)>();
            async Task FlushBufferAsync()
            {
                if (appendBuffer.Count == 0) return;
                await BatchAppendAsync(claimed.SessionId, appendBuffer, ct);
                appendBuffer.Clear();
            }
            var results = await service.SearchProgressivelyAsync(filter, async (candidate, token) =>
            {
                // Every streamed discovery card is stage-bound until a real scrape
                // proves its price, stock and image; snippets alone stay unverified.
                try
                {
                    await verificationQueue.StageAsync(claimed.SessionId, candidate, token);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not stage verification for candidate {Url} in search {SessionId}",
                        candidate.ProductUrl, claimed.SessionId);
                }

                // Appends are serialized through appendLock; buffering small batches
                // and committing them together cuts the per-candidate cost from a
                // session read + sequence max + save each to one transaction per
                // batch, which otherwise delays streaming completion noticeably.
                await appendLock.WaitAsync(token);
                try
                {
                    appendBuffer.Add(("candidate", candidate));
                    candidateCount++;
                    if (appendBuffer.Count >= 8) await FlushBufferAsync();
                }
                finally { appendLock.Release(); }
            }, ct, async (provider, exception, token) =>
            {
                logger.LogWarning(exception, "Discovery provider {Provider} failed for search {SessionId}", provider, claimed.SessionId);
                await appendLock.WaitAsync(token);
                try
                {
                    failures++;
                    await AppendAsync(claimed.SessionId, "sourceFailed", new
                    {
                        source = provider,
                        reason = "This discovery source is temporarily unavailable.",
                        retryable = exception is HttpRequestException
                    }, token);
                }
                finally { appendLock.Release(); }
            });

            var resultList = results.ToList();
            // Drain whatever the providers had queued when they all finished, so
            // the completion event never precedes the last candidates.
            await FlushBufferAsync();
            await FinishSearchAsync(claimed.SessionId,
                new SearchCompletionPayload(resultList.Count, resultList.Count(x => x.IsVerified), candidateCount,
                    failures > 0, failures, service.HasMoreSearchPages(filter.Market, filter.Page)), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search session {SessionId} failed on attempt {Attempt}", claimed.SessionId, claimed.Attempts);
            if (claimed.Attempts < 3 && claimed.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(1))
            {
                var waitSeconds = claimed.Attempts == 1 ? 5 : 20;
                await SetRetryAsync(claimed.SessionId, waitSeconds, ct);
                await AppendAsync(claimed.SessionId, "retrying", new { attempt = claimed.Attempts, retryAfterSeconds = waitSeconds }, ct);
            }
            else
            {
                await FailSearchAsync(claimed.SessionId, new
                {
                    code = "SEARCH_FAILED",
                    message = "Search could not be completed.",
                    partialResults = candidateCount > 0,
                    candidates = candidateCount
                }, ct);
            }
        }
    }

    // One transaction for a whole batch of events: the session read and the
    // sequence ceiling are paid once instead of per event.
    private async Task BatchAppendAsync(Guid sessionId, IReadOnlyList<(string Type, object Payload)> items, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await db.ProductSearchSessions.SingleOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (session is null || session.Status is "completed" or "failed" or "expired") return;
        var sequence = await db.ProductSearchSessionEvents.Where(x => x.SessionId == sessionId)
            .Select(x => (int?)x.Sequence).MaxAsync(ct) ?? 0;
        var now = DateTime.UtcNow;
        foreach (var (type, payload) in items)
        {
            db.ProductSearchSessionEvents.Add(new ProductSearchSessionEvent
            {
                SessionId = sessionId,
                Sequence = ++sequence,
                EventType = type,
                Payload = JsonSerializer.Serialize(payload, JsonOptions),
                OccurredAtUtc = now
            });
        }
        session.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
    }

    private async Task AppendAsync(Guid sessionId, string type, object payload, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await db.ProductSearchSessions.SingleOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (session is null || session.Status is "completed" or "failed" or "expired") return;
        var sequence = await db.ProductSearchSessionEvents.Where(x => x.SessionId == sessionId)
            .Select(x => (int?)x.Sequence).MaxAsync(ct) ?? 0;
        db.ProductSearchSessionEvents.Add(new ProductSearchSessionEvent
        {
            SessionId = sessionId,
            Sequence = sequence + 1,
            EventType = type,
            Payload = JsonSerializer.Serialize(payload, JsonOptions),
            OccurredAtUtc = DateTime.UtcNow
        });
        session.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task FinishSearchAsync(Guid sessionId, object payload, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await db.ProductSearchSessions
            .FromSqlInterpolated($"SELECT * FROM \"ProductSearchSessions\" WHERE \"SessionId\" = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (session is null) return;
        var now = DateTime.UtcNow;
        var completionPayload = payload;
        if (payload is SearchCompletionPayload summary)
        {
            session.VerifiedCandidates = Math.Max(session.VerifiedCandidates, summary.Verified);
            completionPayload = summary with { Verified = session.VerifiedCandidates };
        }
        session.CompletionPayload = JsonSerializer.Serialize(completionPayload, JsonOptions);
        session.Status = session.PendingVerifications == 0 ? "completed" : "awaiting_verification";
        session.UpdatedAtUtc = now;
        if (session.PendingVerifications == 0)
        {
            await AddTerminalEventAsync(db, sessionId, "completed", session.CompletionPayload, now, ct);
        }
        else
        {
            var sequence = await db.ProductSearchSessionEvents
                .Where(item => item.SessionId == sessionId)
                .Select(item => (int?)item.Sequence)
                .MaxAsync(ct) ?? 0;
            db.ProductSearchSessionEvents.Add(new ProductSearchSessionEvent
            {
                SessionId = sessionId,
                Sequence = sequence + 1,
                EventType = "discoveryCompleted",
                Payload = session.CompletionPayload,
                OccurredAtUtc = now
            });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task FailSearchAsync(Guid sessionId, object payload, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await db.ProductSearchSessions.SingleOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (session is null) return;
        var now = DateTime.UtcNow;
        session.Status = "failed";
        session.UpdatedAtUtc = now;
        await AddTerminalEventAsync(db, sessionId, "failed", JsonSerializer.Serialize(payload, JsonOptions), now, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task AddTerminalEventAsync(ApplicationDbContext db, Guid sessionId, string type, string? payload, DateTime occurredAt, CancellationToken ct)
    {
        var sequence = await db.ProductSearchSessionEvents.Where(x => x.SessionId == sessionId)
            .Select(x => (int?)x.Sequence).MaxAsync(ct) ?? 0;
        db.ProductSearchSessionEvents.Add(new ProductSearchSessionEvent
        {
            SessionId = sessionId,
            Sequence = sequence + 1,
            EventType = type,
            Payload = payload ?? "{}",
            OccurredAtUtc = occurredAt
        });
    }

    private async Task SetRetryAsync(Guid sessionId, int delaySeconds, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await db.ProductSearchSessions.SingleAsync(x => x.SessionId == sessionId, ct);
        session.Status = "queued";
        session.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
        session.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task RecoverInterruptedSessionsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var interrupted = await db.ProductSearchSessions.Where(x => x.Status == "running" && x.ExpiresAtUtc > now).ToListAsync(ct);
        foreach (var session in interrupted)
        {
            if (session.Attempts < 3)
            {
                session.Status = "queued";
                session.NextAttemptAtUtc = now;
            }
            else
            {
                session.Status = "failed";
                var sequence = await db.ProductSearchSessionEvents.Where(x => x.SessionId == session.SessionId)
                    .Select(x => (int?)x.Sequence).MaxAsync(ct) ?? 0;
                db.ProductSearchSessionEvents.Add(new ProductSearchSessionEvent
                {
                    SessionId = session.SessionId,
                    Sequence = sequence + 1,
                    EventType = "failed",
                    Payload = JsonSerializer.Serialize(new { code = "WORKER_RESTARTED", message = "Search could not be completed.", partialResults = true }, JsonOptions),
                    OccurredAtUtc = now
                });
            }
            session.UpdatedAtUtc = now;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task CleanupExpiredAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var expiredBefore = DateTime.UtcNow;
        var expiredSessionIds = await db.ProductSearchSessions.AsNoTracking()
            .Where(x => x.ExpiresAtUtc <= expiredBefore)
            .Select(x => x.SessionId)
            .ToListAsync(ct);
        if (expiredSessionIds.Count == 0) return;

        var reviews = await db.ProductSearchSessionEvents.AsNoTracking()
            .Where(x => expiredSessionIds.Contains(x.SessionId) && x.EventType == "matchReview")
            .Select(x => new { x.Id, x.SessionId })
            .ToListAsync(ct);
        if (reviews.Count == 0)
        {
            await db.ProductSearchSessions.Where(x => expiredSessionIds.Contains(x.SessionId)).ExecuteDeleteAsync(ct);
            return;
        }

        var reviewIds = reviews.Select(x => x.Id).ToArray();
        var resolutionPayloads = await db.ProductSearchSessionEvents.AsNoTracking()
            .Where(x => expiredSessionIds.Contains(x.SessionId) && x.EventType == "matchReviewResolved")
            .Select(x => x.Payload)
            .ToListAsync(ct);
        var resolvedIds = new HashSet<long>();
        foreach (var payload in resolutionPayloads)
        {
            try
            {
                using var json = JsonDocument.Parse(payload);
                if (json.RootElement.TryGetProperty("reviewEventId", out var id) && id.TryGetInt64(out var value))
                    resolvedIds.Add(value);
            }
            catch (JsonException) { }
        }

        var unresolvedSessionIds = reviews.Where(x => !resolvedIds.Contains(x.Id))
            .Select(x => x.SessionId).Distinct().ToArray();
        await db.ProductSearchSessions
            .Where(x => expiredSessionIds.Contains(x.SessionId) && !unresolvedSessionIds.Contains(x.SessionId))
            .ExecuteDeleteAsync(ct);
    }
}
