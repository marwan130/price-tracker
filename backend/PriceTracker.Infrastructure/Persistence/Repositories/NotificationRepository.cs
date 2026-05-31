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
                         .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

    public async Task<int> GetUnreadCountByUserIdAsync(Guid userId)
        => await _context.Notifications
                         .CountAsync(n => n.UserId == userId && n.Status == NotificationStatus.Pending);

    public async Task<bool> ExistsForPriceRecordAsync(Guid trackingId, long priceHistoryId)
        => await _context.Notifications
                         .AnyAsync(n => n.TrackingId == trackingId && n.PriceHistoryId == priceHistoryId);

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
                      .Where(n => n.UserId == userId && n.Status == NotificationStatus.Pending)
                      .ExecuteUpdateAsync(n => n.SetProperty(x => x.Status, NotificationStatus.Sent));
    }
}