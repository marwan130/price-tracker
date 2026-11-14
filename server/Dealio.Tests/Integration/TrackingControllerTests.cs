namespace PriceTracker.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.DTOs.Tracking;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;
using PriceTracker.Infrastructure.Persistence;

public class TrackingControllerTests : TestBase
{
    private async Task<string> GetAuthTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Test User",
            Email = "tracking@example.com",
            Password = "Test123!@#"
        };
        await Client.PostAsJsonAsync("/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "tracking@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/v1/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        return loginResult.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private async Task<Guid> CreateTestProductAsync()
    {
        return await SeedProductAsync();
    }

    [Fact]
    public async Task GetTrackings_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/v1/tracking");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTrackings_WithAuth_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await Client.GetAsync("/v1/tracking");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetTrackings_UsesLatestTwoPriceObservationsForDropAndUpdatedFields()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var productId = await CreateTestProductAsync();
        var listingId = Guid.NewGuid();
        var previousAt = DateTime.UtcNow.AddDays(-1);
        var currentAt = DateTime.UtcNow.AddHours(-1);

        using (var scope = CreateServiceScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await context.Users.SingleAsync(x => x.Email == "tracking@example.com");
            var product = await context.Products.SingleAsync(x => x.ProductId == productId);
            var currency = await context.Currencies.SingleOrDefaultAsync(x => x.Code == "EGP");
            if (currency is null)
            {
                currency = new Currency { Code = "EGP", Name = "Egyptian Pound", Symbol = "E£" };
                context.Currencies.Add(currency);
            }

            var store = new Store { StoreId = Guid.NewGuid(), Name = "Verified store", BaseUrl = "https://store.example", Country = "Egypt", CurrencyCode = "EGP" };
            var listing = new StoreProductListing { ListingId = listingId, ProductId = productId, StoreId = store.StoreId, ProductUrl = "https://store.example/item", Product = product, Store = store };
            context.Stores.Add(store);
            context.StoreProductListings.Add(listing);
            context.PriceHistories.AddRange(
                new PriceHistory { ListingId = listingId, Price = 1500, CurrencyCode = "EGP", RecordedAt = previousAt, ScrapedAt = previousAt },
                new PriceHistory { ListingId = listingId, Price = 1200, CurrencyCode = "EGP", RecordedAt = currentAt, ScrapedAt = currentAt });
            context.UserProductTrackings.Add(new UserProductTracking
            {
                TrackingId = Guid.NewGuid(),
                UserId = user.UserId,
                ProductId = productId,
                ListingId = listingId,
                TargetPrice = 1300,
                CurrencyCode = "EGP",
                User = user,
                Product = product,
                Listing = listing,
                Currency = currency
            });
            await context.SaveChangesAsync();

            var historyRepository = scope.ServiceProvider.GetRequiredService<IPriceHistoryRepository>();
            var pair = await historyRepository.GetLatestTwoByListingIdsAsync([listingId]);
            pair[listingId].Select(price => price.Price).Should().Equal(1200, 1500);
        }

        var response = await Client.GetAsync("/v1/tracking?displayCurrency=EGP");
        var responseContent = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseContent);
        var body = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var tracking = body.GetProperty("data")[0];
        tracking.GetProperty("currentPriceInDisplayCurrency").GetDecimal().Should().Be(1200);
        tracking.GetProperty("previousPriceInDisplayCurrency").GetDecimal().Should().Be(1500);
        tracking.GetProperty("priceDropPercent").GetDecimal().Should().Be(20);
        Math.Abs((tracking.GetProperty("currentPriceUpdatedAt").GetDateTime() - currentAt).TotalMilliseconds).Should().BeLessThan(1);
    }

    [Fact]
    public async Task GetTrackingById_WithValidId_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync();
        var createRequest = new CreateTrackingRequest
        {
            ProductId = productId,
            TargetPrice = 100.00m,
            CurrencyCode = "USD"
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/tracking", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var trackingId = createResult.GetProperty("data").GetProperty("trackingId").GetGuid();
        var response = await Client.GetAsync($"/v1/tracking/{trackingId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("trackingId").GetGuid().Should().Be(trackingId);
    }

    [Fact]
    public async Task GetTrackingById_WithInvalidId_ReturnsNotFound()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var invalidId = Guid.NewGuid();
        var response = await Client.GetAsync($"/v1/tracking/{invalidId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTracking_WithValidData_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync();

        var request = new CreateTrackingRequest
        {
            ProductId = productId,
            TargetPrice = 100.00m,
            CurrencyCode = "USD"
        };
        var response = await Client.PostAsJsonAsync("/v1/tracking", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("trackingId").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTracking_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new CreateTrackingRequest
        {
            ProductId = Guid.NewGuid(),
            TargetPrice = 100.00m,
            CurrencyCode = "USD"
        };
        var response = await Client.PostAsJsonAsync("/v1/tracking", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTracking_WithValidData_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync();
        var createRequest = new CreateTrackingRequest
        {
            ProductId = productId,
            TargetPrice = 100.00m,
            CurrencyCode = "USD"
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/tracking", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var trackingId = createResult.GetProperty("data").GetProperty("trackingId").GetGuid();

        var updateRequest = new UpdateTrackingRequest
        {
            TargetPrice = 50.00m,
            NotifyEmail = true,
            NotifyFurtherDecreaseEmail = true
        };
        var response = await Client.PutAsJsonAsync($"/v1/tracking/{trackingId}", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("targetPrice").GetDecimal().Should().Be(50.00m);
        result.GetProperty("data").GetProperty("notifyEmail").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("notifyFurtherDecreaseEmail").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTracking_WithValidId_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync();
        var createRequest = new CreateTrackingRequest
        {
            ProductId = productId,
            TargetPrice = 100.00m,
            CurrencyCode = "USD"
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/tracking", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var trackingId = createResult.GetProperty("data").GetProperty("trackingId").GetGuid();
        var response = await Client.DeleteAsync($"/v1/tracking/{trackingId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteTracking_WithoutAuth_ReturnsUnauthorized()
    {
        var trackingId = Guid.NewGuid();
        var response = await Client.DeleteAsync($"/v1/tracking/{trackingId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
