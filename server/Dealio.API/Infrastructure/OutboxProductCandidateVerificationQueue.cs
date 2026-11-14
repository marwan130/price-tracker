namespace Dealio.API.Infrastructure;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Dealio.Application.DTOs.Products;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Persistence;

public sealed class OutboxProductCandidateVerificationQueue(ApplicationDbContext db) : IProductCandidateVerificationQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> StageAsync(Guid sessionId, ProductSearchResult candidate, CancellationToken cancellationToken = default, bool requireTitleMatch = true, bool deferVerification = false)
    {
        if (!Uri.TryCreate(candidate.ProductUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            return false;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.ProductSearchSessions
            .FromSqlInterpolated($"SELECT * FROM \"ProductSearchSessions\" WHERE \"SessionId\" = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (session is null || session.Status is not "running" and not "awaiting_verification")
            return false;

        var jobId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var filter = JsonSerializer.Deserialize<ProductFilterRequest>(session.RequestPayload, JsonOptions);
        var payload = new ScrapeJobPayload(
            jobId,
            null,
            uri.AbsoluteUri,
            SearchSessionId: sessionId,
            CandidateName: candidate.Name,
            CandidateStoreName: candidate.StoreName,
            MarketCode: candidate.MarketCode,
            Country: candidate.Country,
            DiscoveryProvider: candidate.DiscoveryProvider,
            RequireTitleMatch: requireTitleMatch,
            MinPrice: filter?.MinPrice,
            MaxPrice: filter?.MaxPrice,
            DeferVerification: deferVerification);
        var envelope = new ScrapeEnvelope<ScrapeJobPayload>(1, Guid.NewGuid(), sessionId, now, payload);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = jobId,
            MessageType = "scrape.job.requested.v1",
            RoutingKey = "scrape.job.requested.v1",
            Payload = JsonSerializer.Serialize(envelope, JsonOptions),
            CreatedAtUtc = now
        });
        session.PendingVerifications++;
        session.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
