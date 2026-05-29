namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class AuthService : IAuthService
{
    private readonly IUserRepository  _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher  _passwordHasher;

    public AuthService( IUserRepository  userRepository, IJwtTokenService jwtTokenService, IPasswordHasher  passwordHasher)
    {
        _userRepository  = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher  = passwordHasher;
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
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        return new AuthResponse
        {
            UserId       = user.UserId,
            Name         = user.Name,
            Email        = user.Email,
            Role         = "User",
            AccessToken  = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken = _jwtTokenService.GenerateRefreshToken(),
            ExpiresIn    = 900
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return new AuthResponse
        {
            UserId       = user.UserId,
            Name         = user.Name,
            Email        = user.Email,
            Role         = "User",
            AccessToken  = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken = _jwtTokenService.GenerateRefreshToken(),
            ExpiresIn    = 900
        };
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        throw new NotImplementedException("Implement after RefreshTokenStore is wired.");
    }

    public async Task LogoutAsync(string refreshToken)
    {
        throw new NotImplementedException("Implement after RefreshTokenStore is wired.");
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
}