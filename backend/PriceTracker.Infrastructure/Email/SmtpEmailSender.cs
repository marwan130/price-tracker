namespace PriceTracker.Infrastructure.Email;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using PriceTracker.Application.Interfaces.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
        => _config = config;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var from = _config["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From is not configured.");
        var host = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
        var port = _config["Smtp:Port"] ?? throw new InvalidOperationException("Smtp:Port is not configured.");
        var username = _config["Smtp:Username"] ?? throw new InvalidOperationException("Smtp:Username is not configured.");
        var password = _config["Smtp:Password"] ?? throw new InvalidOperationException("Smtp:Password is not configured.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            host,
            int.Parse(port),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            username,
            password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}