namespace PriceTracker.Application.DTOs.Auth;

public class AuthResponse
{
    public Guid   UserId       { get; set; }
    public string Name         { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string Role         { get; set; } = string.Empty;
    public string AccessToken  { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int    ExpiresIn    { get; set; }
    public string TokenType    { get; set; } = "Bearer";
}