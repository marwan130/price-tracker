namespace Dealio.API.Infrastructure;

using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Dealio.Application.DTOs.Products;
using Dealio.Domain.Entities;
using Dealio.Domain.Enums;
using Dealio.Domain.Markets;
using Dealio.Application.Scrapers;
using Dealio.Infrastructure.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public sealed class RabbitMqOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "dealio";
    public string Password { get; set; } = string.Empty;
}

public sealed class RabbitMqOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqOutboxWorker> logger) : BackgroundService
{
    private const string WorkExchange = "dealio.work.v1";
    private const string ResultExchange = "dealio.results.v1";
    private const string WorkRoute = "scrape.job.requested.v1";
    private const string ResultQueue = "scrape.results.api.v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogWarning("RabbitMQ workflow is disabled; outbox will not be dispatched.");
            return;
        }

        var retry = TimeSpan.FromSeconds(2);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = options.Value.Host,
                    Port = options.Value.Port,
                    UserName = options.Value.User,
                    Password = options.Value.Password,
                    ClientProvidedName = "dealio-api:scrape-outbox-and-results",
                    AutomaticRecoveryEnabled = true
                };
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                var confirmOptions = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
                await using var publisher = await connection.CreateChannelAsync(confirmOptions, stoppingToken);
                await using var consumerChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await publisher.ExchangeDeclareAsync(WorkExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await consumerChannel.ExchangeDeclareAsync(ResultExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await consumerChannel.ExchangeDeclareAsync("dealio.dead.v1", ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                var deadQueue = await consumerChannel.QueueDeclareAsync("scrape.results.api.dead.v1", durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object?> { ["x-max-length"] = 10_000, ["x-overflow"] = "reject-publish" }, cancellationToken: stoppingToken);
                await consumerChannel.QueueBindAsync(deadQueue.QueueName, "dealio.dead.v1", "scrape.result.#", cancellationToken: stoppingToken);
                await consumerChannel.QueueDeclareAsync(ResultQueue, durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "dealio.dead.v1", ["x-dead-letter-routing-key"] = "scrape.result.invalid.v1", ["x-max-length"] = 10_000, ["x-overflow"] = "reject-publish" },
                    cancellationToken: stoppingToken);
                await consumerChannel.QueueBindAsync(ResultQueue, ResultExchange, "scrape.result.*.v1", cancellationToken: stoppingToken);
                await consumerChannel.BasicQosAsync(0, 4, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(consumerChannel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        await ProcessResultAsync(ea.Body.ToArray(), stoppingToken);
                        await consumerChannel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process scrape result {MessageId}; leaving it for redelivery", ea.BasicProperties.MessageId);
                        if (ex is InvalidDataException or JsonException)
                            await consumerChannel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                        else
                            await consumerChannel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
                    }
                };
                await consumerChannel.BasicConsumeAsync(ResultQueue, autoAck: false, consumer, stoppingToken);
                logger.LogDebug("RabbitMQ scrape outbox publisher and result consumer are ready.");
                retry = TimeSpan.FromSeconds(2);

                while (!stoppingToken.IsCancellationRequested && connection.IsOpen && publisher.IsOpen)
                {
                    await PublishBatchAsync(publisher, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ workflow unavailable; retrying in {Delay}", retry);
                await Task.Delay(retry + TimeSpan.FromMilliseconds(Random.Shared.Next(100, 900)), stoppingToken);
                retry = TimeSpan.FromSeconds(Math.Min(retry.TotalSeconds * 2, 60));
            }
        }
    }

    private async Task PublishBatchAsync(IChannel channel, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var pending = await db.OutboxMessages
            .Where(x => x.PublishedAtUtc == null && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.CreatedAtUtc).Take(25).ToListAsync(ct);
        foreach (var item in pending)
        {
            var props = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = "application/json",
                MessageId = item.Id.ToString(),
                CorrelationId = item.Id.ToString(),
                Type = item.MessageType,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };
            try
            {
                await channel.BasicPublishAsync(WorkExchange, item.RoutingKey, mandatory: true, props, Encoding.UTF8.GetBytes(item.Payload), ct);
                item.PublishedAtUtc = DateTime.UtcNow;
                item.LastError = null;
            }
            catch (Exception ex)
            {
                item.Attempts++;
                item.LastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];
                var seconds = Math.Min(300, Math.Pow(2, Math.Min(item.Attempts, 8)));
                item.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(seconds + Random.Shared.Next(1, 5));
                logger.LogWarning(ex, "Outbox message {OutboxId} publish failed", item.Id);
                break;
            }
        }
        if (pending.Count > 0) await db.SaveChangesAsync(ct);
    }

    private async Task ProcessResultAsync(byte[] body, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<ScrapeEnvelope<ScrapeResultPayload>>(body, JsonOptions)
            ?? throw new InvalidDataException("Empty scrape result envelope.");
        if (envelope.SchemaVersion != 1 || envelope.MessageId == Guid.Empty || envelope.Payload.JobId == Guid.Empty)
            throw new InvalidDataException("Unsupported or malformed scrape result envelope.");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var logicalKey = $"scrape-job:{envelope.Payload.JobId:D}";
        if (await db.InboxMessages.AnyAsync(x => x.MessageId == envelope.MessageId || x.LogicalKey == logicalKey, ct))
        {
            await transaction.CommitAsync(ct);
            return;
        }

        var job = await db.OutboxMessages.SingleOrDefaultAsync(x => x.Id == envelope.Payload.JobId, ct);
        if (job is null) throw new InvalidDataException("Scrape result refers to an unknown job.");
        var requestedJob = JsonSerializer.Deserialize<ScrapeEnvelope<ScrapeJobPayload>>(job.Payload, JsonOptions)
            ?? throw new InvalidDataException("Scrape job payload is invalid.");
        if (requestedJob.Payload.JobId != job.Id)
            throw new InvalidDataException("Scrape job identifier does not match its outbox row.");
        if (envelope.Payload.Outcome is not "completed" and not "failed")
            throw new InvalidDataException("Unknown scrape result outcome.");
        if (requestedJob.Payload.SearchSessionId is { } searchSessionId)
        {
            if (envelope.Payload.SearchSessionId != searchSessionId || envelope.Payload.ListingId is not null)
                throw new InvalidDataException("Search verification result does not match its request.");
            await ProcessSearchCandidateResultAsync(db, transaction, envelope, job, requestedJob, logicalKey, ct);
            return;
        }

        if (requestedJob.Payload.ListingId is not { } requestedListingId
            || envelope.Payload.ListingId != requestedListingId)
            throw new InvalidDataException("Scrape result listing does not match its request.");
        var listing = await db.StoreProductListings.Include(x => x.Product).Include(x => x.Store)
            .SingleOrDefaultAsync(x => x.ListingId == requestedListingId, ct);
        if (listing is null) throw new InvalidDataException("Scrape result refers to an unknown listing.");

        var outcome = envelope.Payload.Outcome;
        if (outcome == "completed" && envelope.Payload.Extraction is { } extracted)
        {
            if (!decimal.TryParse(extracted.Price, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var price)
                || price <= 0 || extracted.Currency is not { Length: 3 } currency || !currency.All(char.IsAsciiLetter)
                || string.IsNullOrWhiteSpace(extracted.Title)
                || !TitleMatches(extracted.Title, listing.Product.Name))
            {
                outcome = "rejected";
                logger.LogWarning("Scrape result for job {JobId} failed price, currency, or product identity validation", envelope.Payload.JobId);
            }
            else
            {
                currency = currency.ToUpperInvariant();
                var currencyExists = await db.Currencies.AnyAsync(x => x.Code == currency, ct);
                var storeCurrency = listing.Store.CurrencyCode;
                if (!MiddleEastMarkets.SupportedCurrencyCodes.Contains(currency) || !currencyExists || !string.IsNullOrWhiteSpace(storeCurrency) && !string.Equals(storeCurrency, currency, StringComparison.OrdinalIgnoreCase))
                {
                    outcome = "rejected";
                    logger.LogWarning("Scrape result for job {JobId} used an unsupported or mismatched currency", envelope.Payload.JobId);
                }
                else
                {
                    var latest = await db.PriceHistories.Where(x => x.ListingId == listing.ListingId)
                        .OrderByDescending(x => x.RecordedAt).FirstOrDefaultAsync(ct);
                    var comparableLastPrice = string.Equals(latest?.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)
                        ? latest?.Price
                        : null;
                    var decision = PriceObservationAnomalyPolicy.Evaluate(comparableLastPrice, price, requestedJob.Payload.ExpectedRecheckPrice, out var anomaly);
                    if (decision == PriceObservationDecision.Recheck)
                    {
                        var verificationJobId = Guid.NewGuid();
                        var occurredAt = DateTime.UtcNow;
                        var verificationPayload = requestedJob.Payload with { JobId = verificationJobId, ExpectedRecheckPrice = price };
                        var verificationEnvelope = new ScrapeEnvelope<ScrapeJobPayload>(
                            1, Guid.NewGuid(), requestedJob.CorrelationId, occurredAt, verificationPayload);
                        db.OutboxMessages.Add(new OutboxMessage
                        {
                            Id = verificationJobId,
                            MessageType = "scrape.job.requested.v1",
                            RoutingKey = WorkRoute,
                            Payload = JsonSerializer.Serialize(verificationEnvelope, JsonOptions),
                            CreatedAtUtc = occurredAt
                        });
                        listing.LastRefreshQueuedAtUtc = occurredAt;
                        job.CompletedAtUtc = occurredAt;
                        job.Outcome = "verification_queued";
                        job.LastError = anomaly;
                        db.ScrapeLogs.Add(new ScrapeLog
                        {
                            StoreId = listing.StoreId,
                            ListingId = listing.ListingId,
                            Status = ScrapeStatus.Partial,
                            ErrorMessage = anomaly,
                            ItemsScraped = 0,
                            StartedAt = envelope.Payload.WorkerStartedAtUtc?.ToUniversalTime() ?? job.CreatedAtUtc,
                            FinishedAt = occurredAt
                        });
                        db.InboxMessages.Add(new InboxMessage { MessageId = envelope.MessageId, LogicalKey = logicalKey, MessageType = "scrape.result.v1" });
                        await db.SaveChangesAsync(ct);
                        await transaction.CommitAsync(ct);
                        logger.LogWarning("Scrape job {JobId} produced {Anomaly}; queued one verification fetch", envelope.Payload.JobId, anomaly);
                        return;
                    }

                    if (decision == PriceObservationDecision.Reject)
                    {
                        outcome = "rejected";
                        job.LastError = anomaly;
                        listing.LastScrapedAt = DateTime.UtcNow;
                    }
                    else if (latest is null || latest.Price != price || !string.Equals(latest.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
                    {
                        db.PriceHistories.Add(new PriceHistory
                        {
                            ListingId = listing.ListingId, Price = price, CurrencyCode = currency,
                            RecordedAt = DateTime.UtcNow, ScrapedAt = envelope.OccurredAtUtc
                        });
                    }
                    if (decision == PriceObservationDecision.Accept) listing.HasVerifiedPrice = true;
                    listing.LastScrapedAt = DateTime.UtcNow;
                    await RefreshStoredProductImageAsync(db, listing, extracted.ImageUrl, ct);
                }
            }
        }
        else if (outcome == "failed")
        {
            logger.LogWarning("Scrape job {JobId} failed: {FailureCode}", envelope.Payload.JobId, envelope.Payload.Failure?.Code);
        }
        else if (outcome == "completed")
        {
            outcome = "rejected";
            job.LastError = "result_extraction_missing";
        }
        else throw new InvalidDataException("Scrape result has an unsupported outcome.");

        var completedAtUtc = DateTime.UtcNow;
        var autoSuspended = StoreScrapeHealthPolicy.RecordResult(            listing.Store, succeeded: outcome == "completed", nowUtc: completedAtUtc);
        if (autoSuspended)
            logger.LogWarning("Store {StoreId} was temporarily suspended after {FailureCount} consecutive scrape failures until {SuspendedUntilUtc}",
                listing.StoreId, listing.Store.ConsecutiveScrapeFailures, listing.Store.AutoSuspendedUntilUtc);

        if (outcome is "failed" or "rejected" && !listing.HasVerifiedPrice)
        {
            var latest = await db.PriceHistories.Where(x => x.ListingId == listing.ListingId)
                .OrderByDescending(x => x.RecordedAt).FirstOrDefaultAsync(ct);
            var trackings = await db.UserProductTrackings
                .Where(x => x.IsActive && (x.ListingId == listing.ListingId
                    || !x.ListingId.HasValue && x.ProductId == listing.ProductId))
                .ToListAsync(ct);
            foreach (var tracking in trackings)
            {
                tracking.IsActive = false;
                if (latest is null) continue;
                db.Notifications.Add(new Notification
                {
                    TrackingId = tracking.TrackingId,
                    UserId = tracking.UserId,
                    PriceHistoryId = latest.Id,
                    TriggeredPrice = latest.Price,
                    TargetPrice = tracking.TargetPrice,
                    Channel = NotificationChannel.InApp,
                    EventType = NotificationEventType.TrackingStopped,
                    Status = NotificationStatus.Sent,
                    SentAt = completedAtUtc
                });
            }
            listing.IsActive = false;
        }

        job.CompletedAtUtc = completedAtUtc;
        job.Outcome = outcome;
        db.ScrapeLogs.Add(new ScrapeLog
        {
            StoreId = listing.StoreId,
            ListingId = listing.ListingId,
            Status = outcome == "completed" ? ScrapeStatus.Success : ScrapeStatus.Failed,
            ErrorMessage = outcome switch
            {
                "failed" => envelope.Payload.Failure?.Code ?? "scrape_failed",
                "rejected" => job.LastError ?? "result_validation_failed",
                _ => null
            },
            ItemsScraped = outcome == "completed" ? 1 : 0,
            StartedAt = envelope.Payload.WorkerStartedAtUtc?.ToUniversalTime() ?? job.CreatedAtUtc,
            FinishedAt = job.CompletedAtUtc
        });
        db.InboxMessages.Add(new InboxMessage { MessageId = envelope.MessageId, LogicalKey = logicalKey, MessageType = "scrape.result.v1" });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        if (outcome == "completed")
        {
            try
            {
                await scope.ServiceProvider.GetRequiredService<Dealio.Application.Interfaces.Services.IPriceAlertService>()
                    .EvaluateListingAsync(listing.ListingId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Target evaluation failed after accepted scrape for listing {ListingId}; recurring evaluation will retry", listing.ListingId);
            }
        }
    }

    /// <summary>
    /// The scraper's curl_cffi fetch is the only path that reliably reads
    /// retailer pages (plain HttpClient fetches are TLS-fingerprint blocked),
    /// so each accepted scrape is the moment the stored image URL can be
    /// renewed against the live page. URL rows only, never image bytes.
    /// </summary>
    private async Task RefreshStoredProductImageAsync(
        ApplicationDbContext db, StoreProductListing listing, string? imageUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)
            || Dealio.Application.Scrapers.ScraperHelpers.IsJunkImageUrl(imageUrl)
            || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri)
            || imageUri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(imageUri.UserInfo))
            return; // A void, junk or unfetchable image never replaces a stored one.

        var clean = imageUri.ToString();
        var existing = await db.ProductImages
            .Where(x => x.ProductId == listing.ProductId)
            .OrderByDescending(x => x.IsPrimary)
            .FirstOrDefaultAsync(ct);
        if (existing is null)
            db.ProductImages.Add(new Domain.Entities.ProductImage { ProductId = listing.ProductId, Url = clean, IsPrimary = true });
        else if (!string.Equals(existing.Url, clean, StringComparison.Ordinal))
            existing.Url = clean;
    }

    private async Task ProcessSearchCandidateResultAsync(
        ApplicationDbContext db,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        ScrapeEnvelope<ScrapeResultPayload> envelope,
        OutboxMessage job,
        ScrapeEnvelope<ScrapeJobPayload> requestedJob,
        string logicalKey,
        CancellationToken ct)
    {
        var payload = requestedJob.Payload;
        var session = await db.ProductSearchSessions
            .FromSqlInterpolated($"SELECT * FROM \"ProductSearchSessions\" WHERE \"SessionId\" = {payload.SearchSessionId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        var verified = false;
        ProductSearchResult? candidate = null;
        ProductSearchResult? unverifiedCandidate = null;
        string? failureReason = null;

        if (envelope.Payload.Outcome == "completed" && envelope.Payload.Extraction is { } extracted
            && decimal.TryParse(extracted.Price, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var price)
            && price > 0
            && extracted.Currency is { Length: 3 } currency && currency.All(char.IsAsciiLetter)
            && !string.IsNullOrWhiteSpace(extracted.Title)
            && (!payload.RequireTitleMatch || (!string.IsNullOrWhiteSpace(payload.CandidateName)
                && SearchCandidateTitleMatcher.Matches(extracted.Title, payload.CandidateName))))
        {
            currency = currency.ToUpperInvariant();
            var marketInfo = MiddleEastMarkets.Find(payload.MarketCode);
            var currencyInfo = marketInfo ?? MiddleEastMarkets.All.FirstOrDefault(item => item.CurrencyCode == currency);
            var currencyConfigured = await db.Currencies.AnyAsync(x => x.Code == currency, ct);
            if (MiddleEastMarkets.SupportedCurrencyCodes.Contains(currency)
                && (string.IsNullOrWhiteSpace(payload.MarketCode) || marketInfo is not null)
                && (marketInfo is null ? currencyConfigured || currencyInfo is not null : marketInfo.CurrencyCode == currency)
                && Dealio.Application.Scrapers.ScraperHelpers.IsWithinPriceRange(price, payload.MinPrice, payload.MaxPrice)
                && Uri.TryCreate(payload.Url, UriKind.Absolute, out var uri)
                // Scraped pages can embed a real product's data (JSON-LD) inside a
                // category or marketing page; only URLs that are product pages
                // themselves may reach a verified result.
                && Dealio.Application.Scrapers.ScraperHelpers.IsProductDetailUrl(payload.Url))
            {
                if (!currencyConfigured && currencyInfo is not null)
                {
                    db.Currencies.Add(new Currency { Code = currency, Name = currencyInfo.CurrencyName, Symbol = currencyInfo.CurrencySymbol });
                    await db.SaveChangesAsync(ct);
                }
                candidate = new ProductSearchResult
                {
                    Name = extracted.Title,
                    Description = "",
                    ImageUrl = Dealio.Application.Scrapers.ScraperHelpers.IsJunkImageUrl(extracted.ImageUrl)
                        ? ""
                        : (extracted.ImageUrl ?? ""),
                    Price = price,
                    Currency = currency,
                    StoreName = uri.Host,
                    ProductUrl = payload.Url,
                    InStock = MapAvailability(extracted.Availability),
                    MarketCode = payload.MarketCode ?? "",
                    Country = payload.Country ?? "",
                    DiscoveryProvider = payload.DiscoveryProvider,
                    IsVerified = true
                };
                if (payload.DeferVerification)
                {
                    candidate.IsVerified = false;
                    unverifiedCandidate = candidate;
                    candidate = null;
                }
                else verified = true;
            }
            else failureReason = !Dealio.Application.Scrapers.ScraperHelpers.IsProductDetailUrl(payload.Url)
                ? "candidate_url_not_product_page"
                : Dealio.Application.Scrapers.ScraperHelpers.IsWithinPriceRange(price, payload.MinPrice, payload.MaxPrice)
                    ? "currency_or_market_mismatch"
                    : "price_out_of_range";
        }
        else failureReason = envelope.Payload.Outcome == "failed"
            ? envelope.Payload.Failure?.Code ?? "candidate_fetch_failed"
            : GetCandidateExtractionFailureReason(envelope.Payload.Extraction, payload);

        if (!verified && envelope.Payload.Outcome == "completed" && envelope.Payload.Extraction is { } partial
            && !string.IsNullOrWhiteSpace(partial.Title)
            && Uri.TryCreate(payload.Url, UriKind.Absolute, out var partialUri)
            && partialUri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(partialUri.UserInfo)
            // Same guard as the verified path: a partial extraction pulled from a
            // catalog or marketing page is never a trackable single product.
            && Dealio.Application.Scrapers.ScraperHelpers.IsProductDetailUrl(partialUri.AbsoluteUri))
        {
            var hasPrice = decimal.TryParse(partial.Price, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var partialPrice) && partialPrice > 0;
            var partialInStock = MapAvailability(partial.Availability);
            var partialCurrency = partial.Currency is { Length: 3 } code && code.All(char.IsAsciiLetter) ? code.ToUpperInvariant() : string.Empty;
            var imageUrl = Uri.TryCreate(partial.ImageUrl, UriKind.Absolute, out var imageUri)
                && imageUri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(imageUri.UserInfo)
                && !imageUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                && !imageUri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
                && !imageUri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
                && !imageUri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
                && !Dealio.Application.Scrapers.ScraperHelpers.IsJunkImageUrl(imageUri.ToString())
                ? imageUri.ToString() : string.Empty;
            // An explicitly sold-out page is worth streaming even without a price or
            // image: the user still gets the product with its "out of stock" state,
            // and the tracker picks up the price on the next scrape if it returns.
            if ((hasPrice || imageUrl.Length > 0 || partialInStock is false)
                && (!hasPrice || Dealio.Application.Scrapers.ScraperHelpers.IsWithinPriceRange(partialPrice, payload.MinPrice, payload.MaxPrice)))
                unverifiedCandidate = new ProductSearchResult
                {
                    Name = partial.Title,
                    Description = string.Empty,
                    ImageUrl = imageUrl,
                    Price = hasPrice ? partialPrice : 0,
                    Currency = partialCurrency,
                    StoreName = partialUri.Host,
                    ProductUrl = payload.Url,
                    InStock = partialInStock,
                    MarketCode = payload.MarketCode ?? string.Empty,
                    Country = payload.Country ?? string.Empty,
                    DiscoveryProvider = payload.DiscoveryProvider,
                    IsVerified = false
                };
        }

        var now = DateTime.UtcNow;
        job.CompletedAtUtc = now;
        job.Outcome = verified ? "verified" : unverifiedCandidate is not null ? "extracted" : "rejected";
        job.LastError = failureReason;

        if (session is not null && session.Status is not "completed" and not "failed" and not "expired")
        {
            session.PendingVerifications = Math.Max(0, session.PendingVerifications - 1);
            if (verified) session.VerifiedCandidates++;
            else if (unverifiedCandidate is null)
            {
                await AddSearchSessionEventAsync(db, session.SessionId, "sourceFailed", new
                {
                    source = payload.CandidateStoreName ?? "product page",
                    reason = "This store page did not return usable product details.",
                    retryable = envelope.Payload.Failure?.Retryable ?? false
                }, now, ct);
            }

            if (verified)
            {
                var attached = await AttachRediscoveredOfferAsync(db, session, candidate!, now, ct);
                if (!attached && session.SourceProductId is not null)
                    await AddSearchSessionEventAsync(db, session.SessionId, "matchReview", new
                    {
                        sourceProductId = session.SourceProductId,
                        candidate,
                        reason = "title_or_variant_requires_operator_review"
                    }, now, ct);
                await AddSearchSessionEventAsync(db, session.SessionId, "candidate", candidate!, now, ct);
            }
            else if (unverifiedCandidate is not null)
                await AddSearchSessionEventAsync(db, session.SessionId, "candidate", unverifiedCandidate, now, ct);

            if (session.Status == "awaiting_verification" && session.PendingVerifications == 0)
            {
                session.Status = "completed";
                if (session.CompletionPayload is not null)
                {
                    var summary = JsonSerializer.Deserialize<SearchCompletionPayload>(session.CompletionPayload, JsonOptions)
                        ?? new SearchCompletionPayload(0, 0, 0, true, 0);
                    session.CompletionPayload = JsonSerializer.Serialize(summary with
                    {
                        Verified = session.VerifiedCandidates,
                        Partial = summary.Partial || !verified && unverifiedCandidate is null,
                        FailedSources = summary.FailedSources + (verified || unverifiedCandidate is not null ? 0 : 1)
                    }, JsonOptions);
                }
                await AddSearchSessionEventAsync(db, session.SessionId, "completed",
                    session.CompletionPayload is null ? new SearchCompletionPayload(0, session.VerifiedCandidates, 0, false, 0)
                        : JsonSerializer.Deserialize<SearchCompletionPayload>(session.CompletionPayload, JsonOptions)!, now, ct);
            }
            session.UpdatedAtUtc = now;
        }

        db.InboxMessages.Add(new InboxMessage { MessageId = envelope.MessageId, LogicalKey = logicalKey, MessageType = "scrape.result.v1" });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Search candidate {Outcome} for session {SessionId}: {Reason}", job.Outcome, payload.SearchSessionId, failureReason ?? "candidate extraction complete");
    }

    private static string GetCandidateExtractionFailureReason(ScrapeExtraction? extracted, ScrapeJobPayload payload)
    {
        if (extracted is null) return "candidate_extraction_missing";
        if (!decimal.TryParse(extracted.Price, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
            return "candidate_price_missing_or_invalid";
        if (extracted.Currency is not { Length: 3 } currency || !currency.All(char.IsAsciiLetter))
            return "candidate_currency_missing_or_invalid";
        if (string.IsNullOrWhiteSpace(extracted.Title)) return "candidate_title_missing";
        if (payload.RequireTitleMatch && (string.IsNullOrWhiteSpace(payload.CandidateName)
            || !SearchCandidateTitleMatcher.Matches(extracted.Title, payload.CandidateName)))
            return "candidate_title_mismatch";
        return "candidate_extraction_unverified";
    }

    // Availability arrives as the schema.org JSON-LD tail (InStock, SoldOut,
    // OutOfStock, ...). Tri-state keeps "unknown" distinct from "in stock": a null
    // stays "availability unknown" on the card, an explicit sold-out variant maps
    // to false and still streams as an out-of-stock candidate without a price.
    private static bool? MapAvailability(string? availability) => availability?.Trim() switch
    {
        "InStock" or "LimitedAvailability" or "PreOrder" => true,
        "OutOfStock" or "SoldOut" or "Discontinued" => false,
        _ => (bool?)null
    };

    private static async Task<bool> AttachRediscoveredOfferAsync(
        ApplicationDbContext db, ProductSearchSession session, ProductSearchResult candidate, DateTime now, CancellationToken ct)
    {
        if (session.SourceProductId is not { } productId
            || !Uri.TryCreate(candidate.ProductUrl, UriKind.Absolute, out var uri)
            || !string.Equals(NormalizeTitle(candidate.Name), NormalizeTitle(
                await db.Products.Where(x => x.ProductId == productId).Select(x => x.Name).SingleOrDefaultAsync(ct) ?? ""), StringComparison.Ordinal))
            return false;

        var currency = candidate.Currency.ToUpperInvariant();
        var marketInfo = MiddleEastMarkets.Find(session.MarketCode);
        var currencyInfo = marketInfo ?? MiddleEastMarkets.All.FirstOrDefault(item => item.CurrencyCode == currency);
        if (!MiddleEastMarkets.SupportedCurrencyCodes.Contains(currency)
            || !string.IsNullOrWhiteSpace(session.MarketCode) && marketInfo is null)
            return false;
        if (currencyInfo is not null && !await db.Currencies.AnyAsync(x => x.Code == currency, ct))
        {
            db.Currencies.Add(new Currency { Code = currency, Name = currencyInfo.CurrencyName, Symbol = currencyInfo.CurrencySymbol });
            await db.SaveChangesAsync(ct);
        }
        if (!await db.Currencies.AnyAsync(x => x.Code == currency, ct)) return false;
        var product = await db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, ct);
        if (product is null) return false;
        var existingUrl = await db.StoreProductListings.AnyAsync(x => x.ProductId == productId && x.ProductUrl == candidate.ProductUrl, ct);
        if (existingUrl) return false;

        var baseUrl = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        var country = marketInfo?.Code ?? candidate.Country;
        var store = await db.Stores.FirstOrDefaultAsync(x => x.BaseUrl.ToLower() == baseUrl.ToLower() && x.Country == country, ct);
        if (store is null)
        {
            store = new Store { StoreId = Guid.NewGuid(), Name = uri.Host, BaseUrl = baseUrl, Country = country, CurrencyCode = currency, IsActive = true };
            db.Stores.Add(store);
            await db.SaveChangesAsync(ct);
        }
        if (!string.Equals(store.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) return false;

        var listing = new StoreProductListing
        {
            ListingId = Guid.NewGuid(), ProductId = productId, StoreId = store.StoreId,
            ProductUrl = candidate.ProductUrl, IsActive = candidate.InStock != false, LastScrapedAt = now
        };
        db.StoreProductListings.Add(listing);
        db.PriceHistories.Add(new PriceHistory
        {
            ListingId = listing.ListingId, Price = candidate.Price, CurrencyCode = currency, RecordedAt = now, ScrapedAt = now
        });
        return true;
    }

    private static string NormalizeTitle(string value) => string.Join(' ', value.ToLowerInvariant()
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        .Select(token => new string(token.Where(char.IsLetterOrDigit).ToArray()))
        .Where(token => token.Length > 1));

    private static async Task AddSearchSessionEventAsync(
        ApplicationDbContext db, Guid sessionId, string type, object payload, DateTime now, CancellationToken ct)
    {
        var persistedSequence = await db.ProductSearchSessionEvents.Where(x => x.SessionId == sessionId)
            .Select(x => (int?)x.Sequence).MaxAsync(ct) ?? 0;
        var pendingSequence = db.ProductSearchSessionEvents.Local.Where(x => x.SessionId == sessionId)
            .Select(x => x.Sequence).DefaultIfEmpty(0).Max();
        var sequence = Math.Max(persistedSequence, pendingSequence);
        db.ProductSearchSessionEvents.Add(new ProductSearchSessionEvent
        {
            SessionId = sessionId,
            Sequence = sequence + 1,
            EventType = type,
            Payload = JsonSerializer.Serialize(payload, JsonOptions),
            OccurredAtUtc = now
        });
    }

    private static bool TitleMatches(string extracted, string expected)
    {
        static string Normalize(string value) => string.Join(' ', value.ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => new string(token.Where(char.IsLetterOrDigit).ToArray()))
            .Where(token => token.Length > 1));
        var actual = Normalize(extracted);
        var wanted = Normalize(expected);
        if (actual.Length < 8 || wanted.Length < 8) return false;
        return actual.Contains(wanted, StringComparison.Ordinal) || wanted.Contains(actual, StringComparison.Ordinal);
    }
}
