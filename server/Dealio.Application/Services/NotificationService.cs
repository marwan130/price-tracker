namespace Dealio.Application.Services;

using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.Notifications;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Domain.Exceptions;

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

    public async Task MarkAsReadAsync(long notificationId, Guid userId)
    {
        var updated = await _notificationRepository.MarkAsReadAsync(notificationId, userId);
        if (updated) return;
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification is null) throw new NotFoundException(nameof(Notification), notificationId);
        throw new ForbiddenException("You do not have access to this notification.");
    }

    private static NotificationResponse MapToResponse(Notification notification) => new()
    {
        NotificationId = notification.NotificationId,
        ProductId      = notification.Tracking?.ProductId,
        TrackingId     = notification.TrackingId,
        ListingId      = notification.PriceHistory?.ListingId ?? notification.Tracking?.ListingId,
        ProductName    = notification.Tracking?.Product?.Name        ?? string.Empty,
        StoreName      = notification.PriceHistory?.Listing?.Store?.Name ?? notification.Tracking?.Listing?.Store?.Name ?? string.Empty,
        TriggeredPrice = notification.TriggeredPrice,
        ConvertedTriggeredPrice = notification.ConvertedTriggeredPrice,
        ConversionRate = notification.ConversionRate,
        RateUpdatedAtUtc = notification.RateUpdatedAtUtc,
        TriggeredCurrencyCode = notification.PriceHistory?.CurrencyCode ?? string.Empty,
        TargetCurrencyCode = notification.Tracking?.CurrencyCode ?? string.Empty,
        TargetPrice    = notification.TargetPrice,
        CurrencyCode   = notification.Tracking?.CurrencyCode         ?? string.Empty,
        Channel        = notification.Channel,
        Status         = notification.Status,
        IsRead         = notification.IsRead,
        SentAt         = notification.SentAt
    };
}
