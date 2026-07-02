namespace PriceTracker.Application.Services;

using Microsoft.Extensions.Configuration;
using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using PriceTracker.Domain.Exceptions;

public class AuthService : IAuthService
{
    private const string InvalidVerificationTokenMessage = "Invalid or expired verification token.";
    private const string InvalidResetTokenMessage        = "Invalid or expired reset token.";
    private const string InvalidRefreshTokenMessage      = "Invalid or expired refresh token.";

    private readonly IUserRepository     _userRepository;
    private readonly IJwtTokenService    _jwtTokenService;
    private readonly IPasswordHasher     _passwordHasher;
    private readonly IEmailSender        _emailSender;
    private readonly ISecureTokenService _secureTokenService;
    private readonly IConfiguration      _configuration;

    public AuthService(
        IUserRepository     userRepository,
        IJwtTokenService    jwtTokenService,
        IPasswordHasher     passwordHasher,
        IEmailSender        emailSender,
        ISecureTokenService secureTokenService,
        IConfiguration      configuration)
    {
        _userRepository     = userRepository;
        _jwtTokenService    = jwtTokenService;
        _passwordHasher     = passwordHasher;
        _emailSender        = emailSender;
        _secureTokenService = secureTokenService;
        _configuration      = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (await _userRepository.ExistsByEmailAsync(email))
            throw new ConflictException($"A user with email '{email}' already exists.");

        var verificationToken = _secureTokenService.GenerateToken();

        var user = new User
        {
            UserId                        = Guid.NewGuid(),
            Name                          = request.Name,
            Email                         = email,
            Phone                         = request.Phone,
            PasswordHash                  = _passwordHasher.Hash(request.Password),
            Role                          = UserRole.User,
            IsActive                      = true,
            EmailVerified                 = false,
            EmailVerificationTokenHash    = _secureTokenService.HashToken(verificationToken),
            EmailVerificationTokenExpiresAt = _secureTokenService.GetEmailVerificationExpiryUtc(),
            CreatedAt                     = DateTime.UtcNow
        };

        await SendVerificationEmailOrFailAsync(user, verificationToken);
        await _userRepository.AddAsync(user);

        return new AuthResponse
        {
            UserId        = user.UserId,
            Name          = user.Name,
            Email         = user.Email,
            Role          = user.Role.ToString(),
            ExpiresIn     = 0,
            EmailVerified = false
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userRepository.GetByEmailAsync(email)
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
            UserId        = user.UserId,
            Name          = user.Name,
            Email         = user.Email,
            Role          = user.Role.ToString(),
            AccessToken   = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken  = refreshToken,
            ExpiresIn     = AccessTokenExpirySeconds(),
            EmailVerified = user.EmailVerified
        };
    }

    public async Task VerifyEmailAsync(string token)
    {
        var user = await FindUserByVerificationTokenAsync(token);
        if (user is null)
            throw new UnauthorizedException(InvalidVerificationTokenMessage);

        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
        await _userRepository.UpdateAsync(user);
    }

    public async Task ResendVerificationEmailAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _userRepository.GetByEmailAsync(normalizedEmail)
            ?? throw new NotFoundException(nameof(User), normalizedEmail);

        if (user.EmailVerified)
            return;

        if (!user.IsActive)
            return;

        var verificationToken = _secureTokenService.GenerateToken();
        user.EmailVerificationTokenHash = _secureTokenService.HashToken(verificationToken);
        user.EmailVerificationTokenExpiresAt = _secureTokenService.GetEmailVerificationExpiryUtc();

        await SendVerificationEmailOrFailAsync(user, verificationToken);
        await _userRepository.UpdateAsync(user);
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedException(InvalidRefreshTokenMessage);

        var userId = _jwtTokenService.GetRefreshTokenUserId(request.RefreshToken)
            ?? throw new UnauthorizedException(InvalidRefreshTokenMessage);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedException(InvalidRefreshTokenMessage);

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!user.EmailVerified)
            throw new UnauthorizedException("Please verify your email before refreshing your session.");

        _jwtTokenService.RevokeRefreshToken(request.RefreshToken);

        return new TokenResponse
        {
            AccessToken  = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken = IssueRefreshToken(user.UserId),
            ExpiresIn    = AccessTokenExpirySeconds()
        };
    }

    public Task LogoutAsync(Guid userId, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Task.CompletedTask;

        var tokenUserId = _jwtTokenService.GetRefreshTokenUserId(refreshToken);
        if (tokenUserId != userId)
            throw new UnauthorizedException(InvalidRefreshTokenMessage);

        _jwtTokenService.RevokeRefreshToken(refreshToken);
        return Task.CompletedTask;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        ClearPasswordResetToken(user);
        await _userRepository.UpdateAsync(user);

        _jwtTokenService.RevokeAllRefreshTokensForUser(userId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !user.IsActive)
            return;

        var resetToken = _secureTokenService.GenerateToken();
        user.PasswordResetTokenHash = _secureTokenService.HashToken(resetToken);
        user.PasswordResetTokenExpiresAt = _secureTokenService.GetPasswordResetExpiryUtc();
        await _userRepository.UpdateAsync(user);

        try
        {
            await SendPasswordResetEmailAsync(user, resetToken);
        }
        catch (Exception ex) when (IsEmailDeliveryException(ex))
        {
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            await _userRepository.UpdateAsync(user);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await FindUserByPasswordResetTokenAsync(request.Token);
        if (user is null)
            throw new UnauthorizedException(InvalidResetTokenMessage);

        if (!user.IsActive)
            throw new UnauthorizedException(InvalidResetTokenMessage);

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            throw new BusinessRuleException("New password must be different from the current password.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        ClearPasswordResetToken(user);
        await _userRepository.UpdateAsync(user);

        _jwtTokenService.RevokeAllRefreshTokensForUser(user.UserId);
    }

    private async Task<User?> FindUserByVerificationTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return await _userRepository.GetByEmailVerificationTokenHashAsync(_secureTokenService.HashToken(token));
    }

    private async Task<User?> FindUserByPasswordResetTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return await _userRepository.GetByPasswordResetTokenHashAsync(_secureTokenService.HashToken(token));
    }

    private static void ClearPasswordResetToken(User user)
    {
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
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
        var expiryHours = _configuration["Auth:EmailVerificationExpiryHours"] ?? "24";
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
                <p style="color:#666;font-size:0.9em;">This link expires in {expiryHours} hours and can only be used once.</p>
            </body>
            </html>
            """;

        await _emailSender.SendAsync(user.Email, "Verify your Smart Price Tracker email", body);
    }

    private async Task SendVerificationEmailOrFailAsync(User user, string token)
    {
        try
        {
            await SendVerificationEmailAsync(user, token);
        }
        catch (Exception ex) when (IsEmailDeliveryException(ex))
        {
            throw new BusinessRuleException("Could not send the verification email. Check the SMTP settings and try again.");
        }
    }

    private static bool IsEmailDeliveryException(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? string.Empty;

        return ex is InvalidOperationException
            or FormatException
            or System.Net.Sockets.SocketException
            or System.IO.IOException
            || typeName.StartsWith("MailKit.", StringComparison.Ordinal)
            || typeName.StartsWith("MimeKit.", StringComparison.Ordinal);
    }

    private async Task SendPasswordResetEmailAsync(User user, string token)
    {
        var resetUrl = BuildPasswordResetUrl(token);
        var expiryHours = _configuration["Auth:PasswordResetExpiryHours"] ?? "1";
        var body = $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color:#6c63ff;">Reset your password</h2>
                <p>Hello {System.Net.WebUtility.HtmlEncode(user.Name)},</p>
                <p>We received a request to reset your password. Click the link below to set a new password.</p>
                <p>
                    <a href="{resetUrl}"
                       style="background:#6c63ff;color:#fff;padding:12px 20px;border-radius:6px;text-decoration:none;display:inline-block;">
                        Reset password
                    </a>
                </p>
                <p style="color:#666;font-size:0.9em;">This link expires in {expiryHours} hour(s), can only be used once, and invalidates any previous reset links.</p>
            </body>
            </html>
            """;

        await _emailSender.SendAsync(user.Email, "Reset your Smart Price Tracker password", body);
    }

    private string BuildVerificationUrl(string token)
    {
        var baseUrl = GetFrontendBaseUrl();
        return $"{baseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";
    }

    private string BuildPasswordResetUrl(string token)
    {
        var baseUrl = GetFrontendBaseUrl();
        return $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
    }

    private string GetFrontendBaseUrl()
    {
        var baseUrl = _configuration["Frontend:BaseUrl"]
            ?? _configuration["PUBLIC_APP_URL"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Frontend:BaseUrl or PUBLIC_APP_URL must be configured for auth email links.");

        if (!baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            baseUrl = $"https://{baseUrl}";

        return baseUrl;
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private int AccessTokenExpirySeconds()
        => int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15") * 60;
}
