namespace Dealio.Application.Services;

using Dealio.Application;
using Dealio.Application.DTOs.Currencies;
using Microsoft.Extensions.Logging;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Domain.Enums;

public class PriceAlertService : IPriceAlertService
{
    private static readonly TimeSpan EmailCooldown = TimeSpan.FromHours(6);
    private readonly ITrackingRepository     _trackingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IListingRepository      _listingRepository;
    private readonly IUserRepository         _userRepository;
    private readonly IEmailSender            _emailSender;
    private readonly ICurrencyConversionService _currencyConversionService;
    private readonly ILogger<PriceAlertService> _logger;

    public PriceAlertService(
        ITrackingRepository     trackingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        INotificationRepository notificationRepository,
        IListingRepository      listingRepository,
        IUserRepository         userRepository,
        IEmailSender            emailSender,
        ICurrencyConversionService currencyConversionService,
        ILogger<PriceAlertService> logger)
    {
        _trackingRepository     = trackingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _notificationRepository = notificationRepository;
        _listingRepository      = listingRepository;
        _userRepository         = userRepository;
        _emailSender            = emailSender;
        _currencyConversionService = currencyConversionService;
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
        var owner = await _userRepository.GetByIdAsync(tracking.UserId);
        if (owner is null || !owner.IsActive || owner.IsTrackingPaused) return;

        var listings = await ResolveListingsAsync(tracking);

        foreach (var listing in listings.Where(l => l.IsActive && !l.IsExcluded))
        {
            if (!listing.HasVerifiedPrice) continue;
            var latest = await _priceHistoryRepository.GetLatestByListingIdAsync(listing.ListingId);
            if (latest is null) continue;

            CurrencyConversionResponse conversion;
            try
            {
                conversion = await _currencyConversionService.ConvertAsync(new CurrencyConversionRequest
                {
                    TargetCurrency = tracking.CurrencyCode,
                    Items = [new CurrencyConversionInput { Amount = latest.Price, CurrencyCode = latest.CurrencyCode }]
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or TaskCanceledException or System.Text.Json.JsonException)
            {
                _logger.LogWarning(ex, "Price alert conversion is unavailable for tracking {TrackingId}", tracking.TrackingId);
                continue;
            }

            var convertedPrice = conversion.Items.FirstOrDefault()?.ConvertedAmount;
            if (convertedPrice is null) continue;
            if (convertedPrice.Value > tracking.TargetPrice) continue;
            if (await _notificationRepository.ExistsForPriceRecordAsync(tracking.TrackingId, latest.Id)) continue;

            var priorEvents = await _notificationRepository.GetForTrackingAndListingAsync(tracking.TrackingId, listing.ListingId);
            var eventType = priorEvents.Count == 0
                ? NotificationEventType.TargetReached
                : NotificationEventType.FurtherDecrease;
            if (priorEvents.Count > 0 && latest.Price >= priorEvents.Min(item => item.TriggeredPrice)) continue;

            var notification = new Notification
            {
                TrackingId     = tracking.TrackingId,
                UserId         = tracking.UserId,
                PriceHistoryId = latest.Id,
                TriggeredPrice = latest.Price,
                ConvertedTriggeredPrice = convertedPrice.Value,
                ConversionRate = conversion.Items[0].ConversionRate,
                RateUpdatedAtUtc = conversion.RateUpdatedAtUtc,
                TargetPrice    = tracking.TargetPrice,
                SentAt         = DateTime.UtcNow,
                Channel        = NotificationChannel.Email,
                EventType      = eventType,
                Status         = NotificationStatus.Pending
            };

            await _notificationRepository.AddAsync(notification);

            var emailEnabled = eventType == NotificationEventType.TargetReached
                ? tracking.NotifyEmail
                : tracking.NotifyFurtherDecreaseEmail;
            if (emailEnabled)
                await TrySendAlertEmailAsync(tracking, notification, listing.ListingId, listing.Store?.Name, latest.CurrencyCode);
            else
            {
                notification.Status = NotificationStatus.Suppressed;
                await _notificationRepository.UpdateAsync(notification);
            }
        }
    }

    public async Task EvaluateListingAsync(Guid listingId)
    {
        var listing = await _listingRepository.GetByIdAsync(listingId);
        if (listing is null) return;
        var active = await _trackingRepository.GetActiveTrackingsAsync();
        var affected = active.Where(tracking => tracking.ListingId == listingId
            || !tracking.ListingId.HasValue && tracking.ProductId == listing.ProductId);
        foreach (var tracking in affected)
            await EvaluateTrackingAsync(tracking.TrackingId);
    }

    public async Task RetryFailedEmailNotificationsAsync()
    {
        var retryBefore = DateTime.UtcNow.AddMinutes(-15);
        var failed = await _notificationRepository.GetFailedEmailNotificationsAsync(retryBefore);

        foreach (var notification in failed)
        {
            if (notification.Tracking is null)
                continue;

            var emailEnabled = notification.EventType == NotificationEventType.TargetReached
                ? notification.Tracking.NotifyEmail
                : notification.Tracking.NotifyFurtherDecreaseEmail;
            if (!emailEnabled) continue;

            var listing = notification.PriceHistory.Listing;
            await TrySendAlertEmailAsync(notification.Tracking, notification, listing.ListingId, listing.Store?.Name, notification.PriceHistory.CurrencyCode);
        }
    }

    private async Task TrySendAlertEmailAsync(
        UserProductTracking tracking,
        Notification        notification,
        Guid                listingId,
        string?             storeName,
        string              triggeredCurrencyCode)
    {
        var user = await _userRepository.GetByIdAsync(tracking.UserId);
        if (user is null)
        {
            notification.Status = NotificationStatus.Failed;
            await _notificationRepository.UpdateAsync(notification);
            return;
        }

        if (!user.IsActive || user.IsTrackingPaused)
        {
            notification.Status = NotificationStatus.Suppressed;
            await _notificationRepository.UpdateAsync(notification);
            return;
        }

        if (!user.EmailVerified)
        {
            _logger.LogDebug("Skipping email alert for unverified user {UserId}", user.UserId);
            return;
        }

        var lastSentAt = await _notificationRepository.GetLastSentEmailAtAsync(tracking.TrackingId, listingId);
        if (lastSentAt.HasValue && lastSentAt.Value > DateTime.UtcNow.Subtract(EmailCooldown))
        {
            notification.Status = NotificationStatus.Suppressed;
            await _notificationRepository.UpdateAsync(notification);
            return;
        }

        var productName = tracking.Product?.Name ?? "your tracked product";
        var subject     = $"Price alert: {productName} is now {notification.TriggeredPrice} {triggeredCurrencyCode}";
        var body        = BuildAlertEmailBody(user.Name, productName, storeName, notification, triggeredCurrencyCode, tracking.CurrencyCode);

        try
        {
            await _emailSender.SendAsync(user.Email, subject, body);
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            _logger.LogInformation(
                "Price alert email sent for tracking {TrackingId}",
                tracking.TrackingId);
        }
        catch (Exception ex)
        {
            notification.Status = NotificationStatus.Failed;
            _logger.LogError(
                ex,
                "Failed to send price alert email for tracking {TrackingId}",
                tracking.TrackingId);
        }

        await _notificationRepository.UpdateAsync(notification);
    }

    private static string BuildAlertEmailBody(
        string   userName,
        string   productName,
        string?  storeName,
        Notification notification,
        string   triggeredCurrencyCode,
        string   targetCurrencyCode)
    {
        var safeUserName = System.Net.WebUtility.HtmlEncode(userName);
        var safeProductName = System.Net.WebUtility.HtmlEncode(productName);
        var safeTargetCurrency = System.Net.WebUtility.HtmlEncode(targetCurrencyCode);
        var safeTriggeredCurrency = System.Net.WebUtility.HtmlEncode(triggeredCurrencyCode);
        var storeLine = string.IsNullOrWhiteSpace(storeName)
            ? string.Empty
            : $"<p>Store: {System.Net.WebUtility.HtmlEncode(storeName)}</p>";
        var conversionLine = notification.ConvertedTriggeredPrice is not null
            && !string.Equals(triggeredCurrencyCode, targetCurrencyCode, StringComparison.OrdinalIgnoreCase)
            ? $"<p>In your target currency: <strong>{notification.ConvertedTriggeredPrice} {safeTargetCurrency}</strong></p>"
                + (notification.RateUpdatedAtUtc.HasValue ? $"<p>Exchange rate updated: {notification.RateUpdatedAtUtc.Value:O}</p>" : string.Empty)
            : string.Empty;

        return $"""
            <p>Hi {safeUserName},</p>
            <p>{(notification.EventType == NotificationEventType.TargetReached ? "A tracked offer reached your target price." : "A tracked offer has fallen further since reaching your target.")}</p>
            <p><strong>{safeProductName}</strong></p>
            {storeLine}
            <p>Current price: <strong>{notification.TriggeredPrice} {safeTriggeredCurrency}</strong></p>
            {conversionLine}
            <p>Your target: {notification.TargetPrice} {safeTargetCurrency}</p>
            <p>— {ProductBrand.Name}</p>
            """;
    }

    private async Task<IEnumerable<StoreProductListing>> ResolveListingsAsync(UserProductTracking tracking)
    {
        if (tracking.ListingId.HasValue)
        {
            var listing = await _listingRepository.GetByIdAsync(tracking.ListingId.Value);
            return listing is { IsExcluded: false } ? [listing] : [];
        }

        return (await _listingRepository.GetByProductIdAsync(tracking.ProductId)).Where(item => !item.IsExcluded);
    }
}
