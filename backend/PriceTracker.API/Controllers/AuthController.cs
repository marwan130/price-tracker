namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
        => _authService = authService;

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AuthResponse>.Ok(result, "User registered successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        await _authService.VerifyEmailAsync(token);
        return Ok(ApiResponse<object>.Ok(null!, "Email verified successfully."));
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailRequest request)
    {
        await _authService.ResendVerificationEmailAsync(request.Email);
        return Ok(ApiResponse<object>.Ok(null!, "Verification email sent."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _authService.LogoutAsync(userId, request.RefreshToken);
        return Ok(ApiResponse<object>.Ok(null!, "Logged out successfully."));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _authService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse<object>.Ok(null!, "Password changed successfully."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<object>.Ok(null!, "If an account exists with this email, a password reset link has been sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<object>.Ok(null!, "Password reset successfully."));
    }
}
