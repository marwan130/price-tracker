namespace PriceTracker.Domain.Entities;

using PriceTracker.Domain.Enums;

public class User
{
    public Guid     UserId       { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string?  Phone        { get; set; }
    public string   Email        { get; set; } = string.Empty;
    public string   PasswordHash { get; set; } = string.Empty;
    public UserRole Role         { get; set; } = UserRole.User;
    public bool     IsActive     { get; set; }
    public bool     EmailVerified { get; set; }
    public string?  EmailVerificationTokenHash { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public string?  PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserProductTracking> Trackings       { get; set; } = new List<UserProductTracking>();
    public ICollection<Notification>        Notifications   { get; set; } = new List<Notification>();
}