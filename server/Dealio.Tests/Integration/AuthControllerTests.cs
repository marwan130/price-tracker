namespace Dealio.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Dealio.Application.DTOs.Auth;

public class AuthControllerTests : TestBase
{
    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Test123!@#"
        };
        var response = await Client.PostAsJsonAsync("/v1/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "duplicate@example.com",
            Password = "Test123!@#"
        };
        await Client.PostAsJsonAsync("/v1/auth/register", request);
        var response = await Client.PostAsJsonAsync("/v1/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "weak"
        };
        var response = await Client.PostAsJsonAsync("/v1/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Test User",
            Email = "login@example.com",
            Password = "Test123!@#"
        };
        await Client.PostAsJsonAsync("/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login@example.com",
            Password = "Test123!@#"
        };
        var response = await Client.PostAsJsonAsync("/v1/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("data").GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!@#"
        };
        var response = await Client.PostAsJsonAsync("/v1/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Test User",
            Email = "refresh@example.com",
            Password = "Test123!@#"
        };
        await Client.PostAsJsonAsync("/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "refresh@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/v1/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        var refreshToken = loginResult.GetProperty("data").GetProperty("refreshToken").GetString();

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken!
        };
        var response = await Client.PostAsJsonAsync("/v1/auth/refresh", refreshRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("data").GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid_token"
        };
        var response = await Client.PostAsJsonAsync("/v1/auth/refresh", refreshRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
