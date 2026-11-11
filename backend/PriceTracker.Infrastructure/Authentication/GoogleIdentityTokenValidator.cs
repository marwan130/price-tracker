namespace PriceTracker.Infrastructure.Authentication;

using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using PriceTracker.Application.Interfaces.Services;

public sealed class GoogleIdentityTokenValidator(IConfiguration configuration) : IGoogleIdentityTokenValidator
{
    public async Task<GoogleIdentity?> ValidateAsync(string credential)
    {
        var clientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(credential)) return null;
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
            return new GoogleIdentity(payload.Subject, payload.Email, payload.Name ?? string.Empty,
                payload.EmailVerified, payload.HostedDomain);
        }
        catch (InvalidJwtException) { return null; }
    }
}
