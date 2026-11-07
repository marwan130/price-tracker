namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
        => await _context.Notifications
                         .AsNoTracking()
                         .Include(n => n.Tracking)
                             .ThenInclude(t => t.Product)
                         .Include(n => n.Tracking)
                             .ThenInclude(t => t.Variant)
                         .Include(n => n.Tracking)
                             .ThenInclude(t => t!.Listing)
                                 .ThenInclude(l => l!.Store)
                         .Include(n => n.PriceHistory)
                             .ThenInclude(p => p.Listing)
                                 .ThenInclude(l => l.Store)
                         .Where(n => n.UserId == userId)
                         .OrderByDescending(n => n.SentAt)
                         .ToListAsync();

    public async Task<Notification?> GetByIdAsync(long notificationId)
        => await _context.Notifications
                         .Include(n => n.Tracking)
                             .ThenInclude(t => t.Product)
                         .Include(n => n.Tracking)
                             .ThenInclude(t => t!.Listing)
                                 .ThenInclude(l => l!.Store)
                         .Include(n => n.PriceHistory)
                             .ThenInclude(p => p.Listing)
                                 .ThenInclude(l => l.Store)
                         .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

    public async Task<int> GetUnreadCountByUserIdAsync(Guid userId)
        => await _context.Notifications
                         .CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<bool> ExistsForPriceRecordAsync(Guid trackingId, long priceHistoryId)
        => await _context.Notifications
                         .AnyAsync(n => n.TrackingId == trackingId && n.PriceHistoryId == priceHistoryId);

    public async Task<IReadOnlyList<Notification>> GetForTrackingAndListingAsync(Guid trackingId, Guid listingId)
        => await _context.Notifications.AsNoTracking()
            .Where(n => n.TrackingId == trackingId && n.PriceHistory.ListingId == listingId)
            .OrderByDescending(n => n.SentAt).ToListAsync();

    public async Task<DateTime?> GetLastSentEmailAtAsync(Guid trackingId, Guid listingId)
        => await _context.Notifications.AsNoTracking()
            .Where(n => n.TrackingId == trackingId && n.PriceHistory.ListingId == listingId
                     && n.Channel == NotificationChannel.Email && n.Status == NotificationStatus.Sent)
            .OrderByDescending(n => n.SentAt).Select(n => (DateTime?)n.SentAt).FirstOrDefaultAsync();

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _context.Notifications
                      .Where(n => n.UserId == userId && !n.IsRead)
                      .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));
    }

    public async Task<bool> MarkAsReadAsync(long notificationId, Guid userId)
    {
        var updated = await _context.Notifications
            .Where(n => n.NotificationId == notificationId && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));
        return updated > 0 || await _context.Notifications.AnyAsync(n => n.NotificationId == notificationId && n.UserId == userId);
    }

    public async Task<IEnumerable<Notification>> GetFailedEmailNotificationsAsync(DateTime? olderThanUtc = null, int limit = 50)
    {
        var query = _context.Notifications
                            .Include(n => n.User)
                            .Include(n => n.Tracking)
                                .ThenInclude(t => t.Product)
                            .Include(n => n.Tracking)
                                .ThenInclude(t => t!.Listing)
                                    .ThenInclude(l => l!.Store)
                            .Include(n => n.PriceHistory)
                                .ThenInclude(p => p.Listing)
                                    .ThenInclude(l => l.Store)
                            .Where(n => (n.Status == NotificationStatus.Failed || n.Status == NotificationStatus.Pending)
                                     && n.Channel == NotificationChannel.Email);

        if (olderThanUtc.HasValue)
            query = query.Where(n => n.SentAt <= olderThanUtc.Value);

        return await query
            .OrderBy(n => n.SentAt)
            .Take(limit)
            .ToListAsync();
    }
}
