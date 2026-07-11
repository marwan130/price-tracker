namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Internal;
using PriceTracker.Application.DTOs.Listings;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class ListingService : IListingService
{
    private readonly IListingRepository        _listingRepository;
    private readonly IProductRepository        _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IStoreRepository          _storeRepository;
    private readonly IProductService           _productService;

    public ListingService(
        IListingRepository        listingRepository,
        IProductRepository        productRepository,
        IProductVariantRepository variantRepository,
        IStoreRepository          storeRepository,
        IProductService           productService)
    {
        _listingRepository = listingRepository;
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _storeRepository   = storeRepository;
        _productService    = productService;
    }

    public async Task<IEnumerable<ScrapeListingResponse>> GetActiveForScrapingAsync(int page = 0, int size = 100)
        => (await _listingRepository.GetActiveListingsAsync(page, size)).Select(MapToScrapeResponse);

    public async Task<IEnumerable<ScrapeListingResponse>> GetActiveForScrapingFilteredAsync(string? query = null, int? categoryId = null, Guid? storeId = null, decimal? minPrice = null, decimal? maxPrice = null, string? currencyCode = null, int page = 0, int size = 100)
        => (await _listingRepository.GetActiveListingsFilteredByPreferencesAsync(query, categoryId, storeId, minPrice, maxPrice, currencyCode, page, size)).Select(MapToScrapeResponse);

    public async Task<IEnumerable<ListingResponse>> GetByProductIdAsync(Guid productId)
        => (await _listingRepository.GetByProductIdAsync(productId)).Select(MapToResponse);

    public async Task<IEnumerable<ListingResponse>> GetByProductUrlAsync(string url)
    {
        var existingListing = await _listingRepository.GetByUrlAsync(url);
        if (existingListing != null)
        {
            return await GetByProductIdAsync(existingListing.ProductId);
        }

        var productResponse = await _productService.GetByUrlAsync(url);
        return await GetByProductIdAsync(productResponse.ProductId);
    }

    public async Task<IEnumerable<ListingResponse>> GetByVariantIdAsync(Guid variantId)
        => (await _listingRepository.GetByVariantIdAsync(variantId)).Select(MapToResponse);

    public async Task<IEnumerable<ListingResponse>> GetByStoreIdAsync(Guid storeId)
        => (await _listingRepository.GetByStoreIdAsync(storeId)).Select(MapToResponse);

    public async Task<ListingResponse> GetByIdAsync(Guid listingId)
    {
        var listing = await _listingRepository.GetByIdAsync(listingId)
            ?? throw new NotFoundException(nameof(StoreProductListing), listingId);

        return MapToResponse(listing);
    }

    public async Task<ListingResponse> CreateAsync(CreateListingRequest request)
    {
        _ = await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        _ = await _variantRepository.GetByIdAsync(request.VariantId)
            ?? throw new NotFoundException(nameof(ProductVariant), request.VariantId);

        _ = await _storeRepository.GetByIdAsync(request.StoreId)
            ?? throw new NotFoundException(nameof(Store), request.StoreId);

        if (await _listingRepository.ExistsAsync(request.VariantId, request.StoreId))
            throw new ConflictException("A listing for this variant at this store already exists.");

        var listing = new StoreProductListing
        {
            ListingId  = Guid.NewGuid(),
            ProductId  = request.ProductId,
            VariantId  = request.VariantId,
            StoreId    = request.StoreId,
            ProductUrl = request.ProductUrl,
            IsActive   = true
        };

        await _listingRepository.AddAsync(listing);
        return MapToResponse(listing);
    }

    public async Task<ListingResponse> UpdateAsync(Guid listingId, UpdateListingRequest request)
    {
        var listing = await _listingRepository.GetByIdAsync(listingId)
            ?? throw new NotFoundException(nameof(StoreProductListing), listingId);

        listing.ProductUrl = request.ProductUrl;
        listing.IsActive   = request.IsActive;

        await _listingRepository.UpdateAsync(listing);
        return MapToResponse(listing);
    }

    public async Task DeleteAsync(Guid listingId)
    {
        var listing = await _listingRepository.GetByIdAsync(listingId)
            ?? throw new NotFoundException(nameof(StoreProductListing), listingId);

        await _listingRepository.DeleteAsync(listing);
    }

    private static ScrapeListingResponse MapToScrapeResponse(StoreProductListing listing) => new()
    {
        ListingId    = listing.ListingId,
        StoreId      = listing.StoreId,
        StoreName    = listing.Store?.Name ?? string.Empty,
        ProductUrl   = listing.ProductUrl,
        CurrencyCode = listing.Store?.CurrencyCode,
        ScraperType  = listing.Store?.ScraperType.ToString() ?? "Html"
    };

    private static ListingResponse MapToResponse(StoreProductListing listing)
    {
        var latestPrice = listing.PriceHistories?.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault();
        return new ListingResponse
        {
            ListingId     = listing.ListingId,
            ProductId     = listing.ProductId,
            ProductName   = listing.Product?.Name  ?? string.Empty,
            VariantId     = listing.VariantId,
            VariantSku    = listing.Variant?.Sku   ?? string.Empty,
            StoreId       = listing.StoreId,
            StoreName     = listing.Store?.Name    ?? string.Empty,
            ProductUrl    = listing.ProductUrl,
            IsActive      = listing.IsActive,
            LastScrapedAt = listing.LastScrapedAt,
            CurrentPrice  = latestPrice?.Price,
            CurrencyCode  = latestPrice?.CurrencyCode,
            Currency      = latestPrice?.CurrencyCode
        };
    }
}