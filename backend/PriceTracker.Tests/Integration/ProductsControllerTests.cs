namespace PriceTracker.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.DTOs.Products;

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
    public async Task GetProducts_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_WithAuth_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetProductById_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // First create a product
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            CategoryId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/products", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var productId = createResult.GetProperty("data").GetProperty("productId").GetGuid();

        // Act
        var response = await Client.GetAsync($"/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("productId").GetGuid().Should().Be(productId);
    }

    [Fact]
    public async Task GetProductById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/v1/products/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new CreateProductRequest
        {
            Name = "New Product",
            Description = "New Description",
            CategoryId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("productId").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateProduct_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Description = "New Description",
            CategoryId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // First create a product
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            CategoryId = 1
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

        // Act
        var response = await Client.PutAsJsonAsync($"/v1/products/{productId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("name").GetString().Should().Be("Updated Product");
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // First create a product
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            CategoryId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/v1/products", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var productId = createResult.GetProperty("data").GetProperty("productId").GetGuid();

        // Act
        var response = await Client.DeleteAsync($"/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteProduct_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
