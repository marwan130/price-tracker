namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Notifications;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> GetByUserIdAsync(Guid userId, PaginationRequest pagination);
    Task<NotificationResponse>              GetByIdAsync(long notificationId, Guid userId);
    Task<int>                               GetUnreadCountAsync(Guid userId);
    Task                                    MarkAllAsReadAsync(Guid userId);
}