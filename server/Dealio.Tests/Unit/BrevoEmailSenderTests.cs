namespace Dealio.Tests.Unit;

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Dealio.Infrastructure.Email;

public sealed class BrevoEmailSenderTests
{
    [Fact]
    public async Task SendAsync_UsesBrevoTransactionalEndpointAndConfiguredSender()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Created));
        var sender = CreateSender(handler);

        await sender.SendAsync("customer@example.test", "Target reached", "<p>New price</p>");

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri!.AbsoluteUri.Should().Be("https://api.brevo.com/v3/smtp/email");
        handler.Request.Headers.GetValues("api-key").Should().ContainSingle().Which.Should().Be("test-api-key");
        using var body = JsonDocument.Parse(handler.Body!);
        body.RootElement.GetProperty("sender").GetProperty("email").GetString().Should().Be("alerts@example.test");
        body.RootElement.GetProperty("to")[0].GetProperty("email").GetString().Should().Be("customer@example.test");
        body.RootElement.GetProperty("subject").GetString().Should().Be("Target reached");
        body.RootElement.GetProperty("htmlContent").GetString().Should().Be("<p>New price</p>");
    }

    [Fact]
    public async Task SendAsync_ThrowsWhenBrevoRejectsRequest()
    {
        var sender = CreateSender(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var act = () => sender.SendAsync("customer@example.test", "Target reached", "<p>New price</p>");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static BrevoEmailSender CreateSender(RecordingHandler handler)
    {
        var values = new Dictionary<string, string?>
        {
            ["Brevo:ApiKey"] = "test-api-key",
            ["Brevo:SenderEmail"] = "alerts@example.test",
            ["Brevo:SenderName"] = "Dealio Tests"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new BrevoEmailSender(new HttpClient(handler), configuration, NullLogger<BrevoEmailSender>.Instance);
    }

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }
}
