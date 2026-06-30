namespace PriceTracker.Application.Services;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using PriceTracker.Domain.Exceptions;

public class AuthService : IAuthService
{
    private readonly IUserRepository  _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher  _passwordHasher;
    private readonly IEmailSender     _emailSender;
    private readonly IConfiguration   _configuration;

    public AuthService(
        IUserRepository  userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher  passwordHasher,
        IEmailSender     emailSender,
        IConfiguration   configuration)
    {
        _userRepository  = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher  = passwordHasher;
        _emailSender     = emailSender;
        _configuration   = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            throw new ConflictException($"A user with email '{request.Email}' already exists.");

        var user = new User
        {
            UserId       = Guid.NewGuid(),
            Name         = request.Name,
            Email        = request.Email,
            Phone        = request.Phone,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role         = UserRole.User,
            IsActive     = true,
            EmailVerified = false,
            CreatedAt    = DateTime.UtcNow
        };

        var verificationToken = GenerateVerificationToken();
        SetVerificationToken(user, verificationToken);

        await _userRepository.AddAsync(user);
        await SendVerificationEmailAsync(user, verificationToken);

        return new AuthResponse
        {
            UserId       = user.UserId,
            Name         = user.Name,
            Email        = user.Email,
            Role         = user.Role.ToString(),
            ExpiresIn    = 0,
            EmailVerified = false
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!user.EmailVerified)
            throw new UnauthorizedException("Please verify your email before logging in.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var refreshToken = IssueRefreshToken(user.UserId);

        return new AuthResponse
        {
            UserId       = user.UserId,
            Name         = user.Name,
            Email        = user.Email,
            Role         = user.Role.ToString(),
            AccessToken  = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken = refreshToken,
            ExpiresIn    = 900,
            EmailVerified = user.EmailVerified
        };
    }

    public async Task VerifyEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedException("Invalid or expired verification token.");

        var tokenHash = HashToken(token);
        var user = await _userRepository.GetByEmailVerificationTokenHashAsync(tokenHash)
            ?? throw new UnauthorizedException("Invalid or expired verification token.");

        if (user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Invalid or expired verification token.");

        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
        await _userRepository.UpdateAsync(user);
    }

    public async Task ResendVerificationEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email)
            ?? throw new NotFoundException(nameof(User), email);

        if (user.EmailVerified)
            return;

        var verificationToken = GenerateVerificationToken();
        SetVerificationToken(user, verificationToken);
        await _userRepository.UpdateAsync(user);
        await SendVerificationEmailAsync(user, verificationToken);
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var userId = _jwtTokenService.GetRefreshTokenUserId(request.RefreshToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!user.EmailVerified)
            throw new UnauthorizedException("Please verify your email before refreshing your session.");

        _jwtTokenService.RevokeRefreshToken(request.RefreshToken);

        return new TokenResponse
        {
            AccessToken  = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken = IssueRefreshToken(user.UserId),
            ExpiresIn    = 900
        };
    }

    public Task LogoutAsync(string refreshToken)
    {
        _jwtTokenService.RevokeRefreshToken(refreshToken);
        return Task.CompletedTask;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _userRepository.UpdateAsync(user);
    }

    private string IssueRefreshToken(Guid userId)
    {
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        _jwtTokenService.SaveRefreshToken(refreshToken, userId);
        return refreshToken;
    }

    private async Task SendVerificationEmailAsync(User user, string token)
    {
        var verificationUrl = BuildVerificationUrl(token);
        var body = $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color:#6c63ff;">Verify your email</h2>
                <p>Hello {System.Net.WebUtility.HtmlEncode(user.Name)},</p>
                <p>Confirm your email address to activate your Smart Price Tracker account.</p>
                <p>
                    <a href="{verificationUrl}"
                       style="background:#6c63ff;color:#fff;padding:12px 20px;border-radius:6px;text-decoration:none;display:inline-block;">
                        Verify email
                    </a>
                </p>
                <p style="color:#666;font-size:0.9em;">This link expires in 24 hours.</p>
            </body>
            </html>
            """;

        await _emailSender.SendAsync(user.Email, "Verify your Smart Price Tracker email", body);
    }

    private string BuildVerificationUrl(string token)
    {
        var baseUrl = _configuration["Frontend:BaseUrl"]
            ?? _configuration["PUBLIC_APP_URL"]
            ?? _configuration["RAILWAY_PUBLIC_DOMAIN"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Frontend:BaseUrl or PUBLIC_APP_URL must be configured for email verification links.");

        if (!baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            baseUrl = $"https://{baseUrl}";

        return $"{baseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";
    }

    private static void SetVerificationToken(User user, string token)
    {
        user.EmailVerificationTokenHash = HashToken(token);
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
    }

    private static string GenerateVerificationToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}