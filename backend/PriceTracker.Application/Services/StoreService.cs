namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Stores;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _storeRepository;

    public StoreService(IStoreRepository storeRepository)
        => _storeRepository = storeRepository;

    public async Task<IEnumerable<StoreResponse>> GetAllAsync()
    {
        var stores = await _storeRepository.GetAllAsync();
        return stores.Select(MapToResponse);
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
        return MapToResponse(store);
    }

    public async Task DeleteAsync(Guid storeId)
    {
        var store = await _storeRepository.GetByIdAsync(storeId)
            ?? throw new NotFoundException(nameof(Store), storeId);

        await _storeRepository.DeleteAsync(store);
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
