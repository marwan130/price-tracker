namespace Dealio.API.Infrastructure;

using System.Text.Json;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Persistence;

public sealed class OutboxListingScrapeQueue(ApplicationDbContext db) : IListingScrapeQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Stage(Guid listingId, string httpsUrl)
    {
        if (!Uri.TryCreate(httpsUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Only validated HTTPS listing URLs can be scheduled for scraping.");

        var now = DateTime.UtcNow;
        var jobId = Guid.NewGuid();
        var envelope = new ScrapeEnvelope<ScrapeJobPayload>(1, Guid.NewGuid(), jobId, now,
            new ScrapeJobPayload(jobId, listingId, uri.AbsoluteUri));
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = jobId,
            MessageType = "scrape.job.requested.v1",
            RoutingKey = "scrape.job.requested.v1",
            Payload = JsonSerializer.Serialize(envelope, JsonOptions),
            CreatedAtUtc = now
        });
        // Stage must be durable by itself: callers like TrackingService.CreateAsync finish
        // without another SaveChanges, which previously swallowed the job silently.
        db.SaveChanges();
    }
}
