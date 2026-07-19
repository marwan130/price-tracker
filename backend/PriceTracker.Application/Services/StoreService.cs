namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Stores;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class StoreService : IStoreService
{
    private const string AllStoresKey = "stores:all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly IStoreRepository _storeRepository;
    private readonly ICacheService?   _cache;

    public StoreService(IStoreRepository storeRepository, ICacheService? cache = null)
    {
        _storeRepository = storeRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<StoreResponse>> GetAllAsync()
    {
        if (_cache is not null)
        {
            var cached = await _cache.GetAsync<List<StoreResponse>>(AllStoresKey);
            if (cached is not null)
                return cached;
        }

        var stores = await _storeRepository.GetAllAsync();
        var result = stores.Select(MapToResponse).ToList();

        if (_cache is not null)
            await _cache.SetAsync(AllStoresKey, result, CacheTtl);

        return result;
    }

    public async Task<StoreResponse> GetByIdAsync(Guid storeId)
    {
        var store = await _storeRepository.GetByIdAsync(storeId)
            ?? throw new NotFoundException(nameof(Store), storeId);

        return MapToResponse(store);
    }

    public async Task<StoreResponse> CreateAsync(CreateStoreRequest request)
    {
        var store = new Store
        {
            StoreId      = Guid.NewGuid(),
            Name         = request.Name,
            BaseUrl      = request.BaseUrl,
            Country      = request.Country,
            CurrencyCode = request.CurrencyCode,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        await _storeRepository.AddAsync(store);

        if (_cache is not null)
            await _cache.RemoveAsync(AllStoresKey);

        return MapToResponse(store);
    }

    public async Task<StoreResponse> UpdateAsync(Guid storeId, UpdateStoreRequest request)
    {
        var store = await _storeRepository.GetByIdAsync(storeId)
            ?? throw new NotFoundException(nameof(Store), storeId);

        store.Name         = request.Name;
        store.BaseUrl      = request.BaseUrl;
        store.Country      = request.Country;
        store.CurrencyCode = request.CurrencyCode;
        store.IsActive     = request.IsActive;
        store.ScraperType  = request.ScraperType;

        await _storeRepository.UpdateAsync(store);

        if (_cache is not null)
            await _cache.RemoveAsync(AllStoresKey);

        return MapToResponse(store);
    }

    public async Task DeleteAsync(Guid storeId)
    {
        var store = await _storeRepository.GetByIdAsync(storeId)
            ?? throw new NotFoundException(nameof(Store), storeId);

        await _storeRepository.DeleteAsync(store);

        if (_cache is not null)
            await _cache.RemoveAsync(AllStoresKey);
    }

    private static StoreResponse MapToResponse(Store store) => new()
    {
        StoreId      = store.StoreId,
        Name         = store.Name,
        BaseUrl      = store.BaseUrl,
        Country      = store.Country,
        CurrencyCode = store.CurrencyCode,
        IsActive     = store.IsActive,
        ScraperType  = store.ScraperType,
        CreatedAt    = store.CreatedAt
    };
}