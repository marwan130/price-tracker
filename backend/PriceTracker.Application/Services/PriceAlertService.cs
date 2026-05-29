namespace PriceTracker.Application.Services;

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

    public PriceAlertService(
        ITrackingRepository     trackingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        INotificationRepository notificationRepository,
        IListingRepository      listingRepository)
    {
        _trackingRepository     = trackingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _notificationRepository = notificationRepository;
        _listingRepository      = listingRepository;
    }

    public async Task EvaluateAllActiveTrackingsAsync()
    {
        var trackings = await _trackingRepository.GetActiveTrackingsAsync();

        foreach (var tracking in trackings)
            await EvaluateTrackingAsync(tracking.TrackingId);
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
        }
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