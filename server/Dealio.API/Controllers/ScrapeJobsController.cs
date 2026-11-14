namespace Dealio.API.Controllers;

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Dealio.API.Extensions;
using Dealio.API.Infrastructure;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Persistence;

[ApiController]
[Route("v1/listings/{listingId:guid}/scrape")]
[Authorize]
[EnableRateLimiting("search")]
public sealed class ScrapeJobsController(ApplicationDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    public async Task<IActionResult> Enqueue(Guid listingId, CancellationToken ct)
    {
        var listing = await db.StoreProductListings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ListingId == listingId && x.IsActive && !x.IsExcluded, ct);
        if (listing is null) return NotFound(new { message = "Active offer not found." });

        var now = DateTime.UtcNow;
        var jobId = Guid.NewGuid();
        var envelope = new ScrapeEnvelope<ScrapeJobPayload>(
            SchemaVersion: 1,
            MessageId: Guid.NewGuid(),
            CorrelationId: jobId,
            OccurredAtUtc: now,
            Payload: new ScrapeJobPayload(jobId, listing.ListingId, listing.ProductUrl));
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = jobId,
            RequestedByUserId = User.GetUserId(),
            MessageType = "scrape.job.requested.v1",
            RoutingKey = "scrape.job.requested.v1",
            Payload = JsonSerializer.Serialize(envelope, JsonOptions),
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync(ct);
        return Accepted(new { jobId, status = "queued", statusUrl = $"/v1/scrape-jobs/{jobId}" });
    }

    [HttpGet("~/v1/scrape-jobs/{jobId:guid}")]
    public async Task<IActionResult> GetStatus(Guid jobId, CancellationToken ct)
    {
        var job = await db.OutboxMessages.AsNoTracking()
            .Where(x => x.Id == jobId && x.RequestedByUserId == User.GetUserId())
            .Select(x => new { x.Id, x.PublishedAtUtc, x.CompletedAtUtc, x.Outcome, x.Attempts })
            .SingleOrDefaultAsync(ct);
        return job is null ? NotFound() : Ok(job);
    }
}
