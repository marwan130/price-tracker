namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public class ScrapeLogService : IScrapeLogService
{
    private readonly IScrapeLogRepository _scrapeLogRepository;

    public ScrapeLogService(IScrapeLogRepository scrapeLogRepository)
        => _scrapeLogRepository = scrapeLogRepository;

    public async Task<PagedResult<ScrapeLog>> GetAllAsync(PaginationRequest pagination)
    {
        var all   = (await _scrapeLogRepository.GetAllAsync()).ToList();
        var paged = all.Skip(pagination.Page * pagination.Size).Take(pagination.Size);
        return PagedResult<ScrapeLog>.From(paged, pagination.Page, pagination.Size, all.Count);
    }

    public async Task<PagedResult<ScrapeLog>> GetByStoreIdAsync(Guid storeId, PaginationRequest pagination)
    {
        var all   = (await _scrapeLogRepository.GetByStoreIdAsync(storeId)).ToList();
        var paged = all.Skip(pagination.Page * pagination.Size).Take(pagination.Size);
        return PagedResult<ScrapeLog>.From(paged, pagination.Page, pagination.Size, all.Count);
    }

    public async Task<PagedResult<ScrapeLog>> GetByStatusAsync(ScrapeStatus status, PaginationRequest pagination)
    {
        var all   = (await _scrapeLogRepository.GetByStatusAsync(status)).ToList();
        var paged = all.Skip(pagination.Page * pagination.Size).Take(pagination.Size);
        return PagedResult<ScrapeLog>.From(paged, pagination.Page, pagination.Size, all.Count);
    }

    public async Task<ScrapeLog?> GetByIdAsync(long logId)
        => await _scrapeLogRepository.GetByIdAsync(logId);

    public async Task<ScrapeLog> CreateAsync(ScrapeLog scrapeLog)
    {
        await _scrapeLogRepository.AddAsync(scrapeLog);
        return scrapeLog;
    }

    public async Task UpdateAsync(ScrapeLog scrapeLog)
        => await _scrapeLogRepository.UpdateAsync(scrapeLog);
}