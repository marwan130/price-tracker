namespace PriceTracker.Domain.Entities;

public class User
{
    public Guid     UserId          { get; set; }
    public string   Name            { get; set; } = string.Empty;
    public string?  Phone           { get; set; }
    public string   Email           { get; set; } = string.Empty;
    public string   PasswordHash    { get; set; } = string.Empty;
    public bool     IsActive        { get; set; } 
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserProductTracking> Trackings       { get; set; } = new List<UserProductTracking>();
    public ICollection<Notification>        Notifications   { get; set; } = new List<Notification>();
}