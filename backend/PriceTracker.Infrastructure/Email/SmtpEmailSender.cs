namespace PriceTracker.Infrastructure.Email;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using PriceTracker.Application.Interfaces.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var from     = _config["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From is not configured.");
        var fromName = _config["Smtp:FromName"] ?? "Smart Price Tracker";
        var host     = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
        var port     = _config["Smtp:Port"] ?? throw new InvalidOperationException("Smtp:Port is not configured.");
        var username = _config["Smtp:Username"] ?? throw new InvalidOperationException("Smtp:Username is not configured.");
        var password = _config["Smtp:Password"] ?? throw new InvalidOperationException("Smtp:Password is not configured.");
        var secureSocketOptions = GetSecureSocketOptions();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient
        {
            Timeout = int.TryParse(_config["Smtp:TimeoutMs"], out var timeoutMs) ? timeoutMs : 30_000
        };

        _logger.LogInformation("Sending email via SMTP {Host}:{Port} from {From} to {To}", host, port, from, to);

        try
        {
            await client.ConnectAsync(host, int.Parse(port), secureSocketOptions);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex,
                "SMTP authentication failed for {Username}. Gmail requires an App Password when 2-Step Verification is enabled.",
                username);
            throw;
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(ex, "SMTP command failed ({StatusCode}): {Message}", ex.StatusCode, ex.Message);
            throw;
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogError(ex, "SMTP protocol error while sending to {To}", to);
            throw;
        }
    }

    private SecureSocketOptions GetSecureSocketOptions()
    {
        var configured = _config["Smtp:SecureSocketOptions"];
        if (Enum.TryParse<SecureSocketOptions>(configured, ignoreCase: true, out var parsed))
            return parsed;

        var port = int.TryParse(_config["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }
}
