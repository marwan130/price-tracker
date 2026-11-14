namespace PriceTracker.Application.DTOs.Users;

public class UpdateUserRequest
{
    public string  Name  { get; set; } = string.Empty;
    public string? Phone { get; set; }
}