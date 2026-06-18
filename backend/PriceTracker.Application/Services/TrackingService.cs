namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Tracking;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class TrackingService : ITrackingService
{
    private readonly ITrackingRepository       _trackingRepository;
    private readonly IProductRepository        _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IListingRepository        _listingRepository;
    private readonly IPriceHistoryRepository   _priceHistoryRepository;

    public TrackingService(
        ITrackingRepository       trackingRepository,
        IProductRepository        productRepository,
        IProductVariantRepository variantRepository,
        IListingRepository        listingRepository,
        IPriceHistoryRepository   priceHistoryRepository)
    {
        _trackingRepository     = trackingRepository;
        _productRepository      = productRepository;
        _variantRepository      = variantRepository;
        _listingRepository      = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
    }

    public async Task<IEnumerable<TrackingResponse>> GetByUserIdAsync(Guid userId)
    {
        var trackings = await _trackingRepository.GetByUserIdAsync(userId);
        var tasks = trackings.Select(async t =>
        {
            decimal? currentPrice = null;
            if (t.ListingId.HasValue)
            {
                var latest = await _priceHistoryRepository.GetLatestByListingIdAsync(t.ListingId.Value);
                currentPrice = latest?.Price;
            }
            return MapToResponse(t, currentPrice);
        });
        return await Task.WhenAll(tasks);
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

        return MapToResponse(tracking, latest?.Price);
    }

    public async Task<TrackingResponse> CreateAsync(Guid userId, CreateTrackingRequest request)
    {
        _ = await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (request.VariantId.HasValue)
            _ = await _variantRepository.GetByIdAsync(request.VariantId.Value)
                ?? throw new NotFoundException(nameof(ProductVariant), request.VariantId.Value);

        if (request.ListingId.HasValue)
            _ = await _listingRepository.GetByIdAsync(request.ListingId.Value)
                ?? throw new NotFoundException(nameof(StoreProductListing), request.ListingId.Value);

        if (await _trackingRepository.ExistsAsync(userId, request.ProductId, request.VariantId, request.ListingId))
            throw new BusinessRuleException("You are already tracking this product with the same configuration.");

        var tracking = new UserProductTracking
        {
            TrackingId   = Guid.NewGuid(),
            UserId       = userId,
            ProductId    = request.ProductId,
            VariantId    = request.VariantId,
            ListingId    = request.ListingId,
            TargetPrice  = request.TargetPrice,
            CurrencyCode = request.CurrencyCode,
            IsActive     = true,
            NotifyEmail  = request.NotifyEmail,
            CreatedAt    = DateTime.UtcNow
        };

        await _trackingRepository.AddAsync(tracking);
        return MapToResponse(tracking, null);
    }

    public async Task<TrackingResponse> UpdateAsync(Guid trackingId, Guid userId, UpdateTrackingRequest request)
    {
        var tracking = await _trackingRepository.GetByIdAsync(trackingId)
            ?? throw new NotFoundException(nameof(UserProductTracking), trackingId);

        if (tracking.UserId != userId)
            throw new ForbiddenException("You do not have access to this tracking.");

        tracking.TargetPrice = request.TargetPrice;
        tracking.NotifyEmail = request.NotifyEmail;

        await _trackingRepository.UpdateAsync(tracking);
        return MapToResponse(tracking, null);
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

    private static TrackingResponse MapToResponse(UserProductTracking tracking, decimal? currentPrice) => new()
    {
        TrackingId   = tracking.TrackingId,
        UserId       = tracking.UserId,
        ProductId    = tracking.ProductId,
        ProductName  = tracking.Product?.Name        ?? string.Empty,
        VariantId    = tracking.VariantId,
        VariantSku   = tracking.Variant?.Sku,
        ListingId    = tracking.ListingId,
        StoreName    = tracking.Listing?.Store?.Name,
        TargetPrice  = tracking.TargetPrice,
        CurrencyCode = tracking.CurrencyCode,
        CurrentPrice = currentPrice,
        IsActive     = tracking.IsActive,
        NotifyEmail  = tracking.NotifyEmail,
        CreatedAt    = tracking.CreatedAt
    };
}