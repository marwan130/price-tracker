namespace PriceTracker.Application.Interfaces.Repositories;

using PriceTracker.Domain.Entities;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
    Task<Notification?>             GetByIdAsync(long notificationId);
    Task<int>                       GetUnreadCountByUserIdAsync(Guid userId);
    Task<bool>                      ExistsForPriceRecordAsync(Guid trackingId, long priceHistoryId);
    Task                            AddAsync(Notification notification);
    Task                            UpdateAsync(Notification notification);
    Task                            MarkAllAsReadAsync(Guid userId);
    Task<IEnumerable<Notification>> GetFailedEmailNotificationsAsync(DateTime? olderThanUtc = null, int limit = 50);
}
