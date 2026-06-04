namespace PriceTracker.Infrastructure.Email;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
        => _config = config;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["Smtp:From"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _config["Smtp:Host"],
            int.Parse(_config["Smtp:Port"]!),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            _config["Smtp:Username"],
            _config["Smtp:Password"]);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}