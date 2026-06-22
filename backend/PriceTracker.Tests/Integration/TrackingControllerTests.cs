namespace PriceTracker.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.DTOs.Tracking;

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
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            CategoryId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/products", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        return createResult.GetProperty("data").GetProperty("productId").GetGuid();
    }

    [Fact]
    public async Task GetTrackings_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/v1/tracking");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTrackings_WithAuth_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/v1/tracking");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetTrackingById_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync();

        // Create a tracking
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

        // Act
        var response = await Client.GetAsync($"/v1/tracking/{trackingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("trackingId").GetGuid().Should().Be(trackingId);
    }

    [Fact]
    public async Task GetTrackingById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/v1/tracking/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTracking_WithValidData_ReturnsSuccess()
    {
        // Arrange
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

        // Act
        var response = await Client.PostAsJsonAsync("/v1/tracking", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created); // wait! CreatedAtAction returns Created (201) or OK? Let's check: in TrackingController it returns CreatedAtAction (201)
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("trackingId").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTracking_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateTrackingRequest
        {
            ProductId = Guid.NewGuid(),
            TargetPrice = 100.00m,
            CurrencyCode = "USD"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/tracking", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTracking_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync();

        // Create a tracking
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
            TargetPrice = 50.00m
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/v1/tracking/{trackingId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("targetPrice").GetDecimal().Should().Be(50.00m);
    }

    [Fact]
    public async Task DeleteTracking_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync();

        // Create a tracking
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

        // Act
        var response = await Client.DeleteAsync($"/v1/tracking/{trackingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteTracking_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var trackingId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/v1/tracking/{trackingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
