namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.PriceHistory;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using PriceTracker.Domain.Exceptions;

public class PriceHistoryService : IPriceHistoryService
{
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IListingRepository      _listingRepository;

    public PriceHistoryService(
        IPriceHistoryRepository priceHistoryRepository,
        IListingRepository      listingRepository)
    {
        _priceHistoryRepository = priceHistoryRepository;
        _listingRepository      = listingRepository;
    }

    public async Task<PagedResult<PriceRecordResponse>> GetByListingIdAsync(
        Guid                     listingId,
        PriceHistoryFilterRequest filter,
        PaginationRequest         pagination)
    {
        var records = (await _priceHistoryRepository.GetByListingIdAsync(listingId, filter)).ToList();
        var paged   = records.Skip(pagination.Page * pagination.Size).Take(pagination.Size);

        return PagedResult<PriceRecordResponse>.From(
            paged.Select(MapToResponse),
            pagination.Page,
            pagination.Size,
            records.Count);
    }

    public async Task<PriceRecordResponse> GetByIdAsync(long id)
    {
        var record = await _priceHistoryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(PriceHistory), id);

        return MapToResponse(record);
    }

    public async Task<PriceTrendResponse> GetTrendAsync(Guid listingId, PriceHistoryFilterRequest filter)
    {
        var listing = await _listingRepository.GetByIdAsync(listingId)
            ?? throw new NotFoundException(nameof(StoreProductListing), listingId);

        var records = (await _priceHistoryRepository.GetByListingIdAsync(listingId, filter)).ToList();

        if (records.Count == 0)
            throw new NotFoundException("No price history found for this listing in the given period.");

        var prices     = records.Select(r => r.Price).ToList();
        var dataPoints = records.Select(r => new PriceDataPoint
        {
            RecordedAt = r.RecordedAt,
            Price      = r.Price
        });

        return new PriceTrendResponse
        {
            ListingId      = listingId,
            ProductName    = listing.Product?.Name ?? string.Empty,
            VariantSku     = listing.Variant?.Sku  ?? string.Empty,
            StoreName      = listing.Store?.Name   ?? string.Empty,
            From           = filter.From ?? records.Min(r => r.RecordedAt),
            To             = filter.To   ?? records.Max(r => r.RecordedAt),
            LowestPrice    = prices.Min(),
            HighestPrice   = prices.Max(),
            AveragePrice   = Math.Round(prices.Average(), 2),
            CurrentPrice   = prices.Last(),
            PriceDropCount = CountPriceDrops(prices),
            Trend          = DetermineTrend(prices),
            DataPoints     = dataPoints
        };
    }

    public async Task<PriceRecordResponse> CreateAsync(CreatePriceRecordRequest request)
    {
        var listing = await _listingRepository.GetByIdAsync(request.ListingId)
            ?? throw new NotFoundException(nameof(StoreProductListing), request.ListingId);

        if (await _priceHistoryRepository.ExistsAsync(request.ListingId, request.RecordedAt))
            throw new ConflictException("A price record for this listing at this timestamp already exists.");

        var record = new PriceHistory
        {
            ListingId    = request.ListingId,
            Price        = request.Price,
            CurrencyCode = request.CurrencyCode,
            PriceInUsd   = request.PriceInUsd,
            RecordedAt   = request.RecordedAt,
            ScrapedAt    = DateTime.UtcNow
        };

        await _priceHistoryRepository.AddAsync(record);

        listing.LastScrapedAt = DateTime.UtcNow;
        await _listingRepository.UpdateAsync(listing);

        return MapToResponse(record);
    }

    private static PriceTrend DetermineTrend(List<decimal> prices)
    {
        if (prices.Count < 2) return PriceTrend.Stable;

        var diff = (prices.Last() - prices.First()) / prices.First() * 100;

        return diff switch
        {
            >  2 => PriceTrend.Rising,
            < -2 => PriceTrend.Declining,
            _    => PriceTrend.Stable
        };
    }

    private static int CountPriceDrops(List<decimal> prices)
    {
        int drops = 0;
        for (int i = 1; i < prices.Count; i++)
            if (prices[i] < prices[i - 1]) drops++;
        return drops;
    }

    private static PriceRecordResponse MapToResponse(PriceHistory record) => new()
    {
        Id           = record.Id,
        ListingId    = record.ListingId,
        Price        = record.Price,
        CurrencyCode = record.CurrencyCode,
        PriceInUsd   = record.PriceInUsd,
        RecordedAt   = record.RecordedAt,
        ScrapedAt    = record.ScrapedAt
    };
}