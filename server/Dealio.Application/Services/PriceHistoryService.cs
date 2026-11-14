namespace Dealio.Application.Services;

using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.PriceHistory;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Domain.Enums;
using Dealio.Domain.Exceptions;

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
        pagination.Page = Math.Max(0, pagination.Page);
        pagination.Size = Math.Clamp(pagination.Size, 1, 200);
        pagination.Page = Math.Min(pagination.Page, int.MaxValue / pagination.Size);
        var (records, total) = await _priceHistoryRepository.GetByListingIdPageAsync(listingId, filter, pagination);

        return PagedResult<PriceRecordResponse>.From(
            records.Select(MapToResponse),
            pagination.Page,
            pagination.Size,
            total);
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

    public Task<IReadOnlyList<RecentPriceDropResponse>> GetRecentDropsAsync(int size)
        => _priceHistoryRepository.GetRecentDropsAsync(Math.Clamp(size, 1, 20));

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
