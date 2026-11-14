namespace PriceTracker.Application.Interfaces.Services;

public sealed record GoogleIdentity(string Subject, string Email, string Name, bool EmailVerified, string? HostedDomain);

public interface IGoogleIdentityTokenValidator
{
    Task<GoogleIdentity?> ValidateAsync(string credential);
}
