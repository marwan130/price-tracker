namespace Dealio.Application.Services;

using Dealio.Application.DTOs.Tracking;
using Dealio.Application.DTOs.Currencies;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Domain.Exceptions;
using Dealio.Domain.Markets;

public class TrackingService : ITrackingService
{
    private readonly ITrackingRepository       _trackingRepository;
    private readonly IProductRepository        _productRepository;
    private readonly IListingRepository        _listingRepository;
    private readonly IPriceHistoryRepository   _priceHistoryRepository;
    private readonly IPriceAlertService        _priceAlertService;
    private readonly IListingScrapeQueue      _listingScrapeQueue;
    private readonly ICurrencyConversionService _currencyConversionService;

    public TrackingService(
        ITrackingRepository       trackingRepository,
        IProductRepository        productRepository,
        IListingRepository        listingRepository,
        IPriceHistoryRepository   priceHistoryRepository,
        IPriceAlertService        priceAlertService,
        IListingScrapeQueue      listingScrapeQueue,
        ICurrencyConversionService currencyConversionService)
    {
        _trackingRepository     = trackingRepository;
        _productRepository      = productRepository;
        _listingRepository      = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _priceAlertService      = priceAlertService;
        _listingScrapeQueue     = listingScrapeQueue;
        _currencyConversionService = currencyConversionService;
    }

    public async Task<IEnumerable<TrackingResponse>> GetByUserIdAsync(Guid userId, string? displayCurrency = null)
    {
        var trackings = (await _trackingRepository.GetByUserIdAsync(userId)).ToArray();
        var listingIds = trackings.Where(tracking => tracking.ListingId.HasValue).Select(tracking => tracking.ListingId!.Value).Distinct().ToArray();
        var recentPrices = await _priceHistoryRepository.GetLatestTwoByListingIdsAsync(listingIds);
        var responses = new List<TrackingResponse>();
        foreach (var tracking in trackings)
        {
            IReadOnlyList<PriceHistory> history = tracking.ListingId.HasValue && recentPrices.TryGetValue(tracking.ListingId.Value, out var prices)
                ? prices
                : Array.Empty<PriceHistory>();
            responses.Add(await MapToResponseAsync(tracking, history.ElementAtOrDefault(0), displayCurrency, history.ElementAtOrDefault(1)));
        }

        return responses;
    }

    public async Task<TrackingResponse> GetByIdAsync(Guid trackingId, Guid userId)
    {
        var tracking = await _trackingRepository.GetByIdAsync(trackingId)
            ?? throw new NotFoundException(nameof(UserProductTracking), trackingId);

        if (tracking.UserId != userId)
            throw new ForbiddenException("You do not have access to this tracking.");

        var latest = tracking.ListingId.HasValue
            ? await _priceHistoryRepository.GetLatestByListingIdAsync(tracking.ListingId.Value)
            : null;
        var previous = latest is null ? null : await _priceHistoryRepository.GetPreviousByListingIdAsync(latest.ListingId, latest.Id);

        return await MapToResponseAsync(tracking, latest, previousPrice: previous);
    }

    public async Task<TrackingResponse> CreateAsync(Guid userId, CreateTrackingRequest request)
    {
        if (!MiddleEastMarkets.SupportedCurrencyCodes.Contains(request.CurrencyCode))
            throw new BusinessRuleException("Choose a supported price currency.");

        var product = await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        StoreProductListing? selectedListing = null;

        if (request.ListingId.HasValue)
        {
            selectedListing = await _listingRepository.GetByIdAsync(request.ListingId.Value)
                ?? throw new NotFoundException(nameof(StoreProductListing), request.ListingId.Value);
            if (selectedListing.ProductId != request.ProductId)
                throw new BusinessRuleException("The selected offer does not belong to this product.");
            if (selectedListing.IsExcluded)
                throw new BusinessRuleException("The selected offer is excluded from monitoring. Include it before creating tracking for that offer.");
        }

        if (await _trackingRepository.ExistsAsync(userId, request.ProductId, request.ListingId))
            throw new BusinessRuleException("You are already tracking this product with the same configuration.");

        var tracking = new UserProductTracking
        {
            TrackingId   = Guid.NewGuid(),
            UserId       = userId,
            ProductId    = request.ProductId,
            ListingId    = request.ListingId,
            TargetPrice  = request.TargetPrice,
            CurrencyCode = request.CurrencyCode,
            Product      = product,
            Listing      = selectedListing,
            IsActive     = request.IsActive,
            NotifyEmail  = request.NotifyEmail,
            NotifyFurtherDecreaseEmail = request.NotifyFurtherDecreaseEmail,
            CreatedAt    = DateTime.UtcNow
        };

        await _trackingRepository.AddAsync(tracking);
        if (tracking.IsActive)
        {
            IEnumerable<StoreProductListing> listingsToQueue;
            if (selectedListing is not null)
                listingsToQueue = [selectedListing];
            else
                listingsToQueue = (await _listingRepository.GetByProductIdAsync(product.ProductId)).Where(listing => listing.IsActive && !listing.IsExcluded);
            foreach (var listing in listingsToQueue)
                _listingScrapeQueue.Stage(listing.ListingId, listing.ProductUrl);
        }
        if (tracking.IsActive)
            await _priceAlertService.EvaluateTrackingAsync(tracking.TrackingId);
        var latest = selectedListing is null
            ? null
            : await _priceHistoryRepository.GetLatestByListingIdAsync(selectedListing.ListingId);
        var previous = latest is null ? null : await _priceHistoryRepository.GetPreviousByListingIdAsync(latest.ListingId, latest.Id);
        return await MapToResponseAsync(tracking, latest, previousPrice: previous);
    }

    public async Task<TrackingResponse> UpdateAsync(Guid trackingId, Guid userId, UpdateTrackingRequest request)
    {
        var tracking = await _trackingRepository.GetByIdAsync(trackingId)
            ?? throw new NotFoundException(nameof(UserProductTracking), trackingId);

        if (tracking.UserId != userId)
            throw new ForbiddenException("You do not have access to this tracking.");

        tracking.TargetPrice = request.TargetPrice;
        tracking.NotifyEmail = request.NotifyEmail;
        if (request.NotifyFurtherDecreaseEmail.HasValue)
            tracking.NotifyFurtherDecreaseEmail = request.NotifyFurtherDecreaseEmail.Value;

        await _trackingRepository.UpdateAsync(tracking);
        var latest = tracking.ListingId.HasValue
            ? await _priceHistoryRepository.GetLatestByListingIdAsync(tracking.ListingId.Value)
            : null;
        var previous = latest is null ? null : await _priceHistoryRepository.GetPreviousByListingIdAsync(latest.ListingId, latest.Id);
        return await MapToResponseAsync(tracking, latest, previousPrice: previous);
    }

    public async Task DeleteAsync(Guid trackingId, Guid userId)
    {
        var tracking = await _trackingRepository.GetByIdAsync(trackingId)
            ?? throw new NotFoundException(nameof(UserProductTracking), trackingId);

        if (tracking.UserId != userId)
            throw new ForbiddenException("You do not have access to this tracking.");

        await _trackingRepository.DeleteAsync(tracking);
    }

    public async Task ActivateAsync(Guid trackingId, Guid userId)
    {
        var tracking = await _trackingRepository.GetByIdAsync(trackingId)
            ?? throw new NotFoundException(nameof(UserProductTracking), trackingId);

        if (tracking.UserId != userId)
            throw new ForbiddenException("You do not have access to this tracking.");

        tracking.IsActive = true;
        IEnumerable<StoreProductListing> listingsToQueue;
        if (tracking.ListingId.HasValue)
        {
            var listing = await _listingRepository.GetByIdAsync(tracking.ListingId.Value);
            listingsToQueue = listing is { IsActive: true, IsExcluded: false } ? [listing] : [];
        }
        else
            listingsToQueue = (await _listingRepository.GetByProductIdAsync(tracking.ProductId)).Where(listing => listing.IsActive && !listing.IsExcluded);

        foreach (var listing in listingsToQueue)
            _listingScrapeQueue.Stage(listing.ListingId, listing.ProductUrl);
        await _trackingRepository.UpdateAsync(tracking);
    }

    public async Task DeactivateAsync(Guid trackingId, Guid userId)
    {
        var tracking = await _trackingRepository.GetByIdAsync(trackingId)
            ?? throw new NotFoundException(nameof(UserProductTracking), trackingId);

        if (tracking.UserId != userId)
            throw new ForbiddenException("You do not have access to this tracking.");

        tracking.IsActive = false;
        await _trackingRepository.UpdateAsync(tracking);
    }

    private async Task<TrackingResponse> MapToResponseAsync(UserProductTracking tracking, PriceHistory? latest, string? requestedDisplayCurrency = null, PriceHistory? previousPrice = null)
    {
        var displayCurrency = MiddleEastMarkets.SupportedCurrencyCodes.Contains(requestedDisplayCurrency ?? string.Empty)
            ? requestedDisplayCurrency!
            : tracking.CurrencyCode;
        decimal? convertedPrice = null;
        decimal? currentPriceInDisplayCurrency = null;
        decimal? targetPriceInDisplayCurrency = null;
        decimal? previousPriceInDisplayCurrency = null;
        decimal? priceDropPercent = null;
        var previous = previousPrice;
        if (latest is not null)
        {
            try
            {
                var conversion = await _currencyConversionService.ConvertAsync(new()
                {
                    TargetCurrency = tracking.CurrencyCode,
                    Items = [new() { Amount = latest.Price, CurrencyCode = latest.CurrencyCode }]
                });
                convertedPrice = conversion.Items.FirstOrDefault()?.ConvertedAmount;
                if (string.Equals(displayCurrency, tracking.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                {
                    currentPriceInDisplayCurrency = convertedPrice;
                    targetPriceInDisplayCurrency = tracking.TargetPrice;
                    if (previous is not null)
                    {
                        previousPriceInDisplayCurrency = string.Equals(previous.CurrencyCode, displayCurrency, StringComparison.OrdinalIgnoreCase)
                            ? previous.Price
                            : (await _currencyConversionService.ConvertAsync(new()
                            {
                                TargetCurrency = displayCurrency,
                                Items = [new() { Amount = previous.Price, CurrencyCode = previous.CurrencyCode }]
                            })).Items.FirstOrDefault()?.ConvertedAmount;
                    }
                }
                else
                {
                    var displayInputs = new List<CurrencyConversionInput>
                    {
                        new() { Amount = latest.Price, CurrencyCode = latest.CurrencyCode },
                        new() { Amount = tracking.TargetPrice, CurrencyCode = tracking.CurrencyCode }
                    };
                    if (previous is not null)
                        displayInputs.Add(new() { Amount = previous.Price, CurrencyCode = previous.CurrencyCode });
                    var displayConversion = await _currencyConversionService.ConvertAsync(new()
                    {
                        TargetCurrency = displayCurrency,
                        Items = displayInputs
                    });
                    currentPriceInDisplayCurrency = displayConversion.Items.ElementAtOrDefault(0)?.ConvertedAmount;
                    targetPriceInDisplayCurrency = displayConversion.Items.ElementAtOrDefault(1)?.ConvertedAmount;
                    previousPriceInDisplayCurrency = displayConversion.Items.ElementAtOrDefault(2)?.ConvertedAmount;
                }
                if (previousPriceInDisplayCurrency is > 0 && currentPriceInDisplayCurrency is not null && previousPriceInDisplayCurrency > currentPriceInDisplayCurrency)
                    priceDropPercent = Math.Round((previousPriceInDisplayCurrency.Value - currentPriceInDisplayCurrency.Value) / previousPriceInDisplayCurrency.Value * 100m, 2);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or TaskCanceledException or System.Text.Json.JsonException)
            {
                // Keep source prices visible until a fresh exchange rate is available.
            }
        }

        return new TrackingResponse
        {
            TrackingId   = tracking.TrackingId,
            UserId       = tracking.UserId,
            ProductId    = tracking.ProductId,
            ProductName  = tracking.Product?.Name        ?? string.Empty,
            CategoryName = tracking.Product?.Category?.Name,
            MarketCountry = tracking.Listing?.Store?.Country,
            ListingId    = tracking.ListingId,
            StoreName    = tracking.Listing?.Store?.Name,
            TargetPrice  = tracking.TargetPrice,
            CurrencyCode = tracking.CurrencyCode,
            CurrentPrice = latest?.Price,
            CurrentPriceCurrencyCode = latest?.CurrencyCode,
            CurrentPriceInTargetCurrency = convertedPrice,
            CurrentPriceInDisplayCurrency = currentPriceInDisplayCurrency,
            TargetPriceInDisplayCurrency = targetPriceInDisplayCurrency,
            PreviousPriceInDisplayCurrency = previousPriceInDisplayCurrency,
            PriceDropPercent = priceDropPercent,
            CurrentPriceUpdatedAt = latest?.RecordedAt,
            IsActive     = tracking.IsActive,
            NotifyEmail  = tracking.NotifyEmail,
            NotifyFurtherDecreaseEmail = tracking.NotifyFurtherDecreaseEmail,
            CreatedAt    = tracking.CreatedAt
        };
    }
}
