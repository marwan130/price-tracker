namespace Dealio.Application.Interfaces.Services;

public interface ISecureTokenService
{
    string GenerateToken();
    string HashToken(string token);
    DateTime GetEmailVerificationExpiryUtc();
    DateTime GetPasswordResetExpiryUtc();
}
