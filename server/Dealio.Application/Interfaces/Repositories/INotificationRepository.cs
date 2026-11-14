namespace Dealio.Application.Interfaces.Repositories;

using Dealio.Domain.Entities;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
    Task<Notification?>             GetByIdAsync(long notificationId);
    Task<int>                       GetUnreadCountByUserIdAsync(Guid userId);
    Task<bool>                      ExistsForPriceRecordAsync(Guid trackingId, long priceHistoryId);
    Task<IReadOnlyList<Notification>> GetForTrackingAndListingAsync(Guid trackingId, Guid listingId);
    Task<DateTime?>                  GetLastSentEmailAtAsync(Guid trackingId, Guid listingId);
    Task                            AddAsync(Notification notification);
    Task                            UpdateAsync(Notification notification);
    Task                            MarkAllAsReadAsync(Guid userId);
    Task<bool>                      MarkAsReadAsync(long notificationId, Guid userId);
    Task<IEnumerable<Notification>> GetFailedEmailNotificationsAsync(DateTime? olderThanUtc = null, int limit = 50);
}
