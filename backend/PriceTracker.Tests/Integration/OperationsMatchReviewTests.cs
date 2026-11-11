namespace PriceTracker.Tests.Integration;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using PriceTracker.Infrastructure.Persistence;

public sealed class OperationsMatchReviewTests : TestBase
{
    private async Task<string> GetAdminTokenAsync()
    {
        const string email = "match-review-admin@example.com";
        await Client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest
        {
            Name = "Match Review Admin", Email = email, Password = "Test123!@#"
        });
        await SetUserRoleAsync(email, UserRole.Admin);
        var response = await Client.PostAsJsonAsync("/v1/auth/login", new LoginRequest { Email = email, Password = "Test123!@#" });
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task AdminCanAcceptOrDismissReviewAndResolutionIsIdempotentlyBlocked()
    {
        var token = await GetAdminTokenAsync();
        var productId = Guid.NewGuid();
        var acceptReview = await SeedReviewAsync(productId, "https://shop.example.eg/product-a", "Candidate phone");
        var dismissReview = await SeedReviewAsync(productId, "https://shop.example.eg/product-b", "Another candidate");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var listed = await Client.GetFromJsonAsync<JsonElement>("/v1/operations/match-reviews");
        listed.GetProperty("data").GetArrayLength().Should().Be(2);

        var accepted = await Client.PostAsJsonAsync($"/v1/operations/match-reviews/{acceptReview.SessionId}/{acceptReview.EventId}/resolve",
            new { accept = true, productId });
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        var repeated = await Client.PostAsJsonAsync($"/v1/operations/match-reviews/{acceptReview.SessionId}/{acceptReview.EventId}/resolve",
            new { accept = true, productId });
        repeated.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var dismissed = await Client.PostAsJsonAsync($"/v1/operations/match-reviews/{dismissReview.SessionId}/{dismissReview.EventId}/resolve",
            new { accept = false });
        dismissed.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.StoreProductListings.CountAsync()).Should().Be(1);
        (await db.PriceHistories.CountAsync()).Should().Be(1);
        (await db.ProductSearchSessionEvents.CountAsync(item => item.EventType == "matchReviewResolved")).Should().Be(2);
        (await db.ProductSearchSessionEvents.CountAsync(item => item.EventType == "matchReview")).Should().Be(2);
        (await Client.GetFromJsonAsync<JsonElement>("/v1/operations/match-reviews")).GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task AdminOperationsSummaryReportsRuntimeAndQueueMetrics()
    {
        var token = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.GetAsync("/v1/operations/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var summary = document.RootElement.GetProperty("data");
        summary.GetProperty("apiRequests").GetProperty("requestCount").GetInt32().Should().BeGreaterThan(0);
        summary.GetProperty("scrapeJobsLast24Hours").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        summary.GetProperty("searchSessionsLast24Hours").ValueKind.Should().Be(JsonValueKind.Array);
        summary.GetProperty("notificationOutcomesLast24Hours").ValueKind.Should().Be(JsonValueKind.Array);
        summary.GetProperty("providerQuota").GetProperty("limit").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task NonAdminCannotReadReviewQueue()
    {
        await Client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest
        {
            Name = "Regular User", Email = "match-review-user@example.com", Password = "Test123!@#"
        });
        var response = await Client.PostAsJsonAsync("/v1/auth/login", new LoginRequest
        {
            Email = "match-review-user@example.com", Password = "Test123!@#"
        });
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            document.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!);

        (await Client.GetAsync("/v1/operations/match-reviews")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(Guid SessionId, long EventId)> SeedReviewAsync(Guid productId, string url, string candidateName)
    {
        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await db.Currencies.AnyAsync(item => item.Code == "EGP"))
            db.Currencies.Add(new Currency { Code = "EGP", Name = "Egyptian Pound", Symbol = "E£" });
        var product = await db.Products.FindAsync(productId);
        if (product is null)
        {
            product = new Product { ProductId = productId, Name = "Source phone" };
            db.Products.Add(product);
        }
        var sessionId = Guid.NewGuid();
        var session = new ProductSearchSession
        {
            SessionId = sessionId,
            SourceProductId = productId,
            MarketCode = "EG",
            RequestPayload = "{}",
            Status = "completed",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };
        var candidate = new ProductSearchResult
        {
            Name = candidateName, Price = 14_999m, Currency = "EGP", StoreName = "shop.example.eg",
            ProductUrl = url, MarketCode = "EG", Country = "EG", IsVerified = true, InStock = true
        };
        var review = new { sourceProductId = productId, candidate, reason = "title_or_variant_requires_operator_review" };
        var reviewEvent = new ProductSearchSessionEvent
        {
            SessionId = sessionId, Sequence = 1, EventType = "matchReview",
            Payload = JsonSerializer.Serialize(review, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            OccurredAtUtc = DateTime.UtcNow
        };
        session.Events.Add(reviewEvent);
        db.ProductSearchSessions.Add(session);
        await db.SaveChangesAsync();
        return (sessionId, reviewEvent.Id);
    }
}
