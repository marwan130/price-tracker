namespace PriceTracker.Application.Services;

using Microsoft.Extensions.Logging;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public class PriceAlertService : IPriceAlertService
{
    private readonly ITrackingRepository     _trackingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IListingRepository      _listingRepository;
    private readonly IUserRepository         _userRepository;
    private readonly IEmailSender            _emailSender;
    private readonly ILogger<PriceAlertService> _logger;

    public PriceAlertService(
        ITrackingRepository     trackingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        INotificationRepository notificationRepository,
        IListingRepository      listingRepository,
        IUserRepository         userRepository,
        IEmailSender            emailSender,
        ILogger<PriceAlertService> logger)
    {
        _trackingRepository     = trackingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _notificationRepository = notificationRepository;
        _listingRepository      = listingRepository;
        _userRepository         = userRepository;
        _emailSender            = emailSender;
        _logger                 = logger;
    }

    public async Task EvaluateAllActiveTrackingsAsync()
    {
        var trackings = await _trackingRepository.GetActiveTrackingsAsync();

        foreach (var tracking in trackings)
            await EvaluateTrackingAsync(tracking.TrackingId);

        await RetryFailedEmailNotificationsAsync();
    }

    public async Task EvaluateTrackingAsync(Guid trackingId)
    {
        var tracking = await _trackingRepository.GetByIdAsync(trackingId);
        if (tracking is null || !tracking.IsActive) return;

        var listings = await ResolveListingsAsync(tracking);

        foreach (var listing in listings.Where(l => l.IsActive))
        {
            var latest = await _priceHistoryRepository.GetLatestByListingIdAsync(listing.ListingId);
            if (latest is null) continue;

            if (latest.Price > tracking.TargetPrice) continue;

            if (await _notificationRepository.ExistsForPriceRecordAsync(tracking.TrackingId, latest.Id))
                continue;

            var notification = new Notification
            {
                TrackingId     = tracking.TrackingId,
                UserId         = tracking.UserId,
                PriceHistoryId = latest.Id,
                TriggeredPrice = latest.Price,
                TargetPrice    = tracking.TargetPrice,
                SentAt         = DateTime.UtcNow,
                Channel        = NotificationChannel.Email,
                Status         = NotificationStatus.Pending
            };

            await _notificationRepository.AddAsync(notification);

            if (tracking.NotifyEmail)
                await TrySendAlertEmailAsync(tracking, notification, listing.Store?.Name);
        }
    }

    public async Task RetryFailedEmailNotificationsAsync()
    {
        var failed = await _notificationRepository.GetFailedEmailNotificationsAsync();

        foreach (var notification in failed)
        {
            if (notification.Tracking is null || !notification.Tracking.NotifyEmail)
                continue;

            var storeName = notification.Tracking.Listing?.Store?.Name;
            await TrySendAlertEmailAsync(notification.Tracking, notification, storeName);
        }
    }

    private async Task TrySendAlertEmailAsync(
        UserProductTracking tracking,
        Notification        notification,
        string?             storeName)
    {
        var user = await _userRepository.GetByIdAsync(tracking.UserId);
        if (user is null)
        {
            notification.Status = NotificationStatus.Failed;
            await _notificationRepository.UpdateAsync(notification);
            return;
        }

        var productName = tracking.Product?.Name ?? "your tracked product";
        var subject     = $"Price alert: {productName} is now {notification.TriggeredPrice} {tracking.CurrencyCode}";
        var body        = BuildAlertEmailBody(user.Name, productName, storeName, notification, tracking.CurrencyCode);

        try
        {
            await _emailSender.SendAsync(user.Email, subject, body);
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            _logger.LogInformation(
                "Price alert email sent to {Email} for tracking {TrackingId}",
                user.Email,
                tracking.TrackingId);
        }
        catch (Exception ex)
        {
            notification.Status = NotificationStatus.Failed;
            _logger.LogError(
                ex,
                "Failed to send price alert email to {Email} for tracking {TrackingId}",
                user.Email,
                tracking.TrackingId);
        }

        await _notificationRepository.UpdateAsync(notification);
    }

    private static string BuildAlertEmailBody(
        string   userName,
        string   productName,
        string?  storeName,
        Notification notification,
        string   currencyCode)
    {
        var storeLine = string.IsNullOrWhiteSpace(storeName)
            ? string.Empty
            : $"<p>Store: {storeName}</p>";

        return $"""
            <p>Hi {userName},</p>
            <p>Good news — a product you're tracking has dropped to your target price.</p>
            <p><strong>{productName}</strong></p>
            {storeLine}
            <p>Current price: <strong>{notification.TriggeredPrice} {currencyCode}</strong></p>
            <p>Your target: {notification.TargetPrice} {currencyCode}</p>
            <p>— Smart Price Tracker</p>
            """;
    }

    private async Task<IEnumerable<StoreProductListing>> ResolveListingsAsync(UserProductTracking tracking)
    {
        if (tracking.ListingId.HasValue)
        {
            var listing = await _listingRepository.GetByIdAsync(tracking.ListingId.Value);
            return listing is not null ? [listing] : [];
        }

        if (tracking.VariantId.HasValue)
            return await _listingRepository.GetByVariantIdAsync(tracking.VariantId.Value);

        return await _listingRepository.GetByProductIdAsync(tracking.ProductId);
    }
}