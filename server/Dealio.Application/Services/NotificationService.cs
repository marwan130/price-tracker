namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Notifications;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
        => _notificationRepository = notificationRepository;

    public async Task<PagedResult<NotificationResponse>> GetByUserIdAsync(
        Guid              userId,
        PaginationRequest pagination)
    {
        var all   = (await _notificationRepository.GetByUserIdAsync(userId)).ToList();
        var paged = all.Skip(pagination.Page * pagination.Size).Take(pagination.Size);

        return PagedResult<NotificationResponse>.From(
            paged.Select(MapToResponse),
            pagination.Page,
            pagination.Size,
            all.Count);
    }

    public async Task<NotificationResponse> GetByIdAsync(long notificationId, Guid userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId)
            ?? throw new NotFoundException(nameof(Notification), notificationId);

        if (notification.UserId != userId)
            throw new ForbiddenException("You do not have access to this notification.");

        return MapToResponse(notification);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
        => await _notificationRepository.GetUnreadCountByUserIdAsync(userId);

    public async Task MarkAllAsReadAsync(Guid userId)
        => await _notificationRepository.MarkAllAsReadAsync(userId);

    private static NotificationResponse MapToResponse(Notification notification) => new()
    {
        NotificationId = notification.NotificationId,
        ProductName    = notification.Tracking?.Product?.Name        ?? string.Empty,
        VariantSku     = notification.Tracking?.Variant?.Sku,
        StoreName      = notification.Tracking?.Listing?.Store?.Name ?? string.Empty,
        TriggeredPrice = notification.TriggeredPrice,
        TargetPrice    = notification.TargetPrice,
        CurrencyCode   = notification.Tracking?.CurrencyCode         ?? string.Empty,
        Channel        = notification.Channel,
        Status         = notification.Status,
        SentAt         = notification.SentAt
    };
}