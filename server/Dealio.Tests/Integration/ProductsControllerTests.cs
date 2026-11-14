namespace Dealio.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Dealio.Application.DTOs.Auth;
using Dealio.Application.DTOs.Products;
using Dealio.Application.DTOs.Tracking;
using Dealio.Application.Interfaces.Services;
using Dealio.API.Infrastructure;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

public class ProductsControllerTests : TestBase
{
    private async Task<string> GetAuthTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Test User",
            Email = "products@example.com",
            Password = "Test123!@#"
        };
        await Client.PostAsJsonAsync("/v1/auth/register", registerRequest);
        await SetUserRoleAsync(registerRequest.Email, Dealio.Domain.Enums.UserRole.Admin);

        var loginRequest = new LoginRequest
        {
            Email = "products@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/v1/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        return loginResult.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task GetProducts_WithoutAuth_ReturnsSuccess()
    {
        var response = await Client.GetAsync("/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("http://store.example/product")]
    [InlineData("https://user:password@store.example/product")]
    public async Task SearchByUrl_RejectsUnsafeUrlBeforeQueueing(string url)
    {
        var response = await Client.PostAsJsonAsync("/v1/products/search/url", new { url });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.ProductSearchSessions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetDiscoveryOverview_WithoutAuth_ReturnsRealDataCollections()
    {
        var response = await Client.GetAsync("/v1/discovery/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        data.GetProperty("trendingProducts").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("popularCategories").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("recentlyAddedProducts").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("recentlyAddedStores").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("bestRecentDeals").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ProductSearchStream_ReplaysPersistedEventsFromLastEventCursor()
    {
        var sessionId = Guid.NewGuid();
        using (var scope = CreateServiceScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ProductSearchSessions.Add(new ProductSearchSession
            {
                SessionId = sessionId,
                RequestPayload = "{\"query\":\"camera\"}",
                Status = "completed",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                Events =
                [
                    new ProductSearchSessionEvent { Sequence = 1, EventType = "candidate", Payload = "{\"name\":\"Camera\"}" },
                    new ProductSearchSessionEvent { Sequence = 2, EventType = "completed", Payload = "{\"total\":1,\"verified\":0}" }
                ]
            });
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/products/search/stream?sessionId={sessionId:D}");
        request.Headers.Add("Last-Event-ID", $"{sessionId:N}:1");
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var stream = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
        stream.Should().Contain($"id: {sessionId:N}:2");
        stream.Should().Contain("event: completed");
        stream.Should().NotContain("event: candidate");
    }

    [Fact]
    public async Task CandidateVerificationQueue_PersistsOutboxJobAndPendingCountAtomically()
    {
        var sessionId = Guid.NewGuid();
        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ProductSearchSessions.Add(new ProductSearchSession
        {
            SessionId = sessionId,
            RequestPayload = "{\"query\":\"camera\"}",
            Status = "running",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        });
        await db.SaveChangesAsync();

        var queue = scope.ServiceProvider.GetRequiredService<IProductCandidateVerificationQueue>();
        var staged = await queue.StageAsync(sessionId, new ProductSearchResult
        {
            Name = "Example Camera 24MP",
            ProductUrl = "https://shop.example/products/camera",
            StoreName = "shop.example",
            MarketCode = "EG",
            Country = "Egypt",
            DiscoveryProvider = "Tavily",
            IsVerified = false
        });

        staged.Should().BeTrue();
        (await db.ProductSearchSessions.Where(x => x.SessionId == sessionId).Select(x => x.PendingVerifications).SingleAsync()).Should().Be(1);
        var outbox = await db.OutboxMessages.Where(x => x.MessageType == "scrape.job.requested.v1").SingleAsync();
        var job = JsonSerializer.Deserialize<ScrapeEnvelope<ScrapeJobPayload>>(outbox.Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        job.Should().NotBeNull();
        job!.Payload.SearchSessionId.Should().Be(sessionId);
        job.Payload.ListingId.Should().BeNull();
        job.Payload.CandidateName.Should().Be("Example Camera 24MP");
        job.Payload.DeferVerification.Should().BeFalse();
    }

    [Fact]
    public async Task ImportUrl_UsesPreviouslyObservedCandidateAndLeavesPriceUnverifiedUntilTracking()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var url = "https://fixture.example/products/verified-camera";
        using (var scope = CreateServiceScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            db.ProductSearchSessions.Add(new ProductSearchSession
            {
                SessionId = Guid.NewGuid(),
                RequestPayload = "{\"query\":\"camera\"}",
                Status = "awaiting_verification",
                PendingVerifications = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ExpiresAtUtc = now.AddHours(1),
                Events =
                [
                    new ProductSearchSessionEvent
                    {
                        Sequence = 1,
                        EventType = "candidate",
                        Payload = JsonSerializer.Serialize(new ProductSearchResult
                        {
                            Name = "Fixture Camera Model 42",
                            ProductUrl = url,
                            StoreName = "Fixture Retailer",
                            ImageUrl = "https://fixture.example/images/camera.jpg",
                            Price = 12500m,
                            Currency = "EGP",
                            MarketCode = "EG",
                            Country = "Egypt",
                            IsVerified = false,
                            DiscoveryProvider = "Scrapling"
                        }, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        using var response = await Client.PostAsJsonAsync("/v1/products/import-url", new { url });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var productId = document.RootElement.GetProperty("data").GetProperty("productId").GetGuid();
        using var verifyScope = CreateServiceScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var listing = await verifyDb.StoreProductListings.SingleAsync(item => item.ProductId == productId);
        listing.HasVerifiedPrice.Should().BeFalse();
        // Unverified candidates carry snippet-mined prices that are not page
        // observations; the seeded price must not enter history until the tracker
        // records its first real scrape.
        (await verifyDb.PriceHistories.CountAsync(item => item.ListingId == listing.ListingId)).Should().Be(0);

        using var trackResponse = await Client.PostAsJsonAsync("/v1/tracking", new CreateTrackingRequest
        {
            ProductId = productId,
            ListingId = listing.ListingId,
            TargetPrice = 10000m,
            CurrencyCode = "EGP",
            NotifyEmail = false,
            NotifyFurtherDecreaseEmail = false
        });
        trackResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var trackingDocument = JsonDocument.Parse(await trackResponse.Content.ReadAsStringAsync());
        trackingDocument.RootElement.GetProperty("data").GetProperty("listingId").GetGuid().Should().Be(listing.ListingId);
        (await verifyDb.UserProductTrackings.CountAsync(item => item.ProductId == productId && item.ListingId == listing.ListingId)).Should().Be(1);
    }

    [Fact]
    public async Task ImportUrl_RejectsUrlWithoutRecentSearchResult()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await Client.PostAsJsonAsync("/v1/products/import-url", new
        {
            url = "https://fixture.example/products/not-verified"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("SEARCH_RESULT_REQUIRED");
    }

    [Fact]
    public async Task GetProducts_WithAuth_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await Client.GetAsync("/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetProductById_WithValidId_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            CategoryId = null
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/products", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var productId = createResult.GetProperty("data").GetProperty("productId").GetGuid();
        var response = await Client.GetAsync($"/v1/products/{productId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("productId").GetGuid().Should().Be(productId);
    }

    [Fact]
    public async Task GetProductById_WithInvalidId_ReturnsNotFound()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var invalidId = Guid.NewGuid();
        var response = await Client.GetAsync($"/v1/products/{invalidId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new CreateProductRequest
        {
            Name = "New Product",
            Description = "New Description",
            CategoryId = null
        };
        var response = await Client.PostAsJsonAsync("/v1/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("productId").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateProduct_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Description = "New Description",
            CategoryId = null
        };
        var response = await Client.PostAsJsonAsync("/v1/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            CategoryId = null
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/products", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var productId = createResult.GetProperty("data").GetProperty("productId").GetGuid();

        var updateRequest = new UpdateProductRequest
        {
            Name = "Updated Product",
            Description = "Updated Description"
        };
        var response = await Client.PutAsJsonAsync($"/v1/products/{productId}", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("name").GetString().Should().Be("Updated Product");
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            CategoryId = null
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/products", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var productId = createResult.GetProperty("data").GetProperty("productId").GetGuid();
        var response = await Client.DeleteAsync($"/v1/products/{productId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteProduct_WithoutAuth_ReturnsUnauthorized()
    {
        var productId = Guid.NewGuid();
        var response = await Client.DeleteAsync($"/v1/products/{productId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
