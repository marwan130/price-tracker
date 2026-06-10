namespace PriceTracker.Application.DTOs.Users;

public class UserResponse
{
    public Guid     UserId    { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string   Email     { get; set; } = string.Empty;
    public string?  Phone     { get; set; }
    public string   Role      { get; set; } = string.Empty;
    public bool     IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}