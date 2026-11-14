namespace Dealio.Tests.Integration;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Dealio.Application.DTOs.Auth;

public sealed class AccountDataControllerTests : TestBase
{
    [Fact]
    public async Task Export_IncludesAccountTrackingOffersAndNotifications()
    {
        await Client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest
        {
            Name = "Portability User",
            Email = "portability@example.com",
            Password = "Test123!@#"
        });
        var login = await Client.PostAsJsonAsync("/v1/auth/login", new LoginRequest
        {
            Email = "portability@example.com",
            Password = "Test123!@#"
        });
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", loginJson.RootElement.GetProperty("data").GetProperty("accessToken").GetString());

        var exported = await Client.GetAsync("/v1/users/me/export?format=json");
        exported.EnsureSuccessStatusCode();
        using var exportJson = JsonDocument.Parse(await exported.Content.ReadAsStringAsync());
        var payload = exportJson.RootElement.GetProperty("data");
        payload.GetProperty("account").GetProperty("email").GetString().Should().Be("portability@example.com");
        payload.GetProperty("trackings").GetArrayLength().Should().Be(0);
        payload.GetProperty("offers").GetArrayLength().Should().Be(0);
        payload.GetProperty("notifications").GetArrayLength().Should().Be(0);
        exported = await Client.GetAsync("/v1/users/me/export?format=json&market=EG&fromUtc=2026-11-01T00:00:00Z&toUtc=2026-11-30T23:59:59Z");
        exported.EnsureSuccessStatusCode();
        using var filteredJson = JsonDocument.Parse(await exported.Content.ReadAsStringAsync());
        filteredJson.RootElement.GetProperty("data").GetProperty("trackings").GetArrayLength().Should().Be(0);
        exported = await Client.GetAsync("/v1/users/me/export?format=csv");
        exported.EnsureSuccessStatusCode();
        (await exported.Content.ReadAsStringAsync()).Should().Contain("trackingId,productId,productName");
    }

    [Fact]
    public async Task Export_RejectsUnsupportedMarketAndReversedDateRange()
    {
        await Client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest
        {
            Name = "Filtered Export User",
            Email = "filtered-export@example.com",
            Password = "Test123!@#"
        });
        var login = await Client.PostAsJsonAsync("/v1/auth/login", new LoginRequest
        {
            Email = "filtered-export@example.com",
            Password = "Test123!@#"
        });
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", loginJson.RootElement.GetProperty("data").GetProperty("accessToken").GetString());

        var unsupportedMarket = await Client.GetAsync("/v1/users/me/export?market=IL");
        unsupportedMarket.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var reversedRange = await Client.GetAsync("/v1/users/me/export?fromUtc=2026-11-05T00:00:00Z&toUtc=2026-11-04T00:00:00Z");
        reversedRange.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
