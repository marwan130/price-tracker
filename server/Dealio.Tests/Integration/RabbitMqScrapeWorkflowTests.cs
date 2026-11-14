namespace Dealio.Tests.Integration;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dealio.Application.DTOs.Auth;
using Dealio.Application.DTOs.Tracking;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Persistence;
using RabbitMQ.Client;

public sealed class RabbitMqScrapeWorkflowTests : TestBase
{
    private const string RabbitUser = "dealio_test";
    private const string RabbitPassword = "dealio_test_password";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RabbitMqScrapeWorkflowTests() : base(enableRabbitMq: true) { }

    [Fact]
    public async Task AuthenticatedTracking_PublishesScrapeAndPersistsOneRabbitResult()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = GetRabbitMqPort(),
            UserName = RabbitUser,
            Password = RabbitPassword
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync("dealio.work.v1", ExchangeType.Topic, durable: true);
        var queue = await channel.QueueDeclareAsync("scrape.jobs.v1", durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queue.QueueName, "dealio.work.v1", "scrape.job.requested.v1");

        var productId = await SeedListingAsync();
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var trackingResponse = await Client.PostAsJsonAsync("/v1/tracking", new CreateTrackingRequest
        {
            ProductId = productId,
            ListingId = await GetListingIdAsync(productId),
            TargetPrice = 90m,
            CurrencyCode = "EGP",
            NotifyEmail = false,
            NotifyFurtherDecreaseEmail = false
        });
        trackingResponse.EnsureSuccessStatusCode();

        var job = await ReceiveJobAsync(channel, queue.QueueName, TimeSpan.FromSeconds(25));
        using var jobDocument = JsonDocument.Parse(job.Body.ToArray());
        var root = jobDocument.RootElement;
        var payload = root.GetProperty("payload");
        var jobId = payload.GetProperty("jobId").GetGuid();
        var listingId = payload.GetProperty("listingId").GetGuid();
        var resultMessageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var result = new
        {
            schemaVersion = 1,
            messageId = resultMessageId,
            correlationId = root.GetProperty("correlationId").GetGuid(),
            causationId = root.GetProperty("messageId").GetGuid(),
            occurredAtUtc = now,
            payload = new
            {
                jobId,
                listingId,
                searchSessionId = (Guid?)null,
                outcome = "completed",
                extraction = new
                {
                    title = "Dealio fixture product model 1000",
                    price = "95.00",
                    currency = "EGP",
                    imageUrl = (string?)null,
                    availability = "in_stock",
                    extractionMethod = "fixture"
                },
                failure = (object?)null,
                workerStartedAtUtc = now,
                durationMs = 5
            }
        };
        await channel.ExchangeDeclareAsync("dealio.results.v1", ExchangeType.Topic, durable: true);
        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json",
            MessageId = resultMessageId.ToString(),
            CorrelationId = root.GetProperty("correlationId").GetGuid().ToString()
        };
        await channel.BasicPublishAsync("dealio.results.v1", "scrape.result.completed.v1", true,
            properties, JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions));

        await EventuallyAsync(async () =>
        {
            using var scope = CreateServiceScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.PriceHistories.CountAsync(x => x.ListingId == listingId && x.Price == 95m) == 1;
        }, TimeSpan.FromSeconds(25));

        using (var scope = CreateServiceScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.PriceHistories.CountAsync(x => x.ListingId == listingId && x.Price == 95m)).Should().Be(1);
            (await db.InboxMessages.CountAsync(x => x.MessageId == resultMessageId)).Should().Be(1);
            (await db.OutboxMessages.Where(x => x.Id == jobId).Select(x => x.Outcome).SingleAsync()).Should().Be("completed");
        }
    }

    private int GetRabbitMqPort()
    {
        using var scope = CreateServiceScope();
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        return configuration.GetValue<int>("RabbitMq:Port");
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        await Client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest
        {
            Name = "Queue Test User",
            Email = "rabbit-workflow@example.com",
            Password = "Test123!@#"
        });
        using var response = await Client.PostAsJsonAsync("/v1/auth/login", new LoginRequest
        {
            Email = "rabbit-workflow@example.com",
            Password = "Test123!@#"
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private async Task<Guid> SeedListingAsync()
    {
        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Currencies.Add(new Currency { Code = "EGP", Name = "Egyptian Pound", Symbol = "E£" });
        var product = new Product { Name = "Dealio fixture product model 1000" };
        var store = new Store
        {
            Name = "Fixture Retailer",
            BaseUrl = "https://fixture.example",
            Country = "EG",
            CurrencyCode = "EGP"
        };
        var listing = new StoreProductListing
        {
            Product = product,
            Store = store,
            ProductUrl = "https://fixture.example/products/model-1000",
            PriceHistories = [new PriceHistory
            {
                Price = 120m,
                CurrencyCode = "EGP",
                RecordedAt = DateTime.UtcNow.AddDays(-1),
                ScrapedAt = DateTime.UtcNow.AddDays(-1)
            }]
        };
        db.StoreProductListings.Add(listing);
        await db.SaveChangesAsync();
        return product.ProductId;
    }

    private async Task<Guid> GetListingIdAsync(Guid productId)
    {
        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.StoreProductListings.Where(x => x.ProductId == productId).Select(x => x.ListingId).SingleAsync();
    }

    private static async Task<BasicGetResult> ReceiveJobAsync(IChannel channel, string queue, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var message = await channel.BasicGetAsync(queue, autoAck: true);
            if (message is not null) return message;
            await Task.Delay(100);
        }
        throw new TimeoutException("The authenticated tracking request was not published to RabbitMQ.");
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return;
            await Task.Delay(150);
        }
        throw new TimeoutException("RabbitMQ result was not persisted by the API consumer.");
    }
}
