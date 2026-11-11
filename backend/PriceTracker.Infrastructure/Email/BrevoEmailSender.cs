namespace PriceTracker.Infrastructure.Email;

using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Application;

public sealed class BrevoEmailSender(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<BrevoEmailSender> logger) : IEmailSender
{
    private const string Endpoint = "https://api.brevo.com/v3/smtp/email";

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var apiKey = configuration["Brevo:ApiKey"];
        var senderEmail = configuration["Brevo:SenderEmail"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
            throw new InvalidOperationException("Brevo:ApiKey and Brevo:SenderEmail must be configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.TryAddWithoutValidation("api-key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");
        request.Content = JsonContent.Create(new
        {
            sender = new { email = senderEmail, name = configuration["Brevo:SenderName"] ?? ProductBrand.Name },
            to = new[] { new { email = to } },
            subject,
            htmlContent = htmlBody
        });

        using var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Brevo transactional email request failed with HTTP {StatusCode}",
                (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        logger.LogDebug("Transactional email accepted by provider");
    }
}
