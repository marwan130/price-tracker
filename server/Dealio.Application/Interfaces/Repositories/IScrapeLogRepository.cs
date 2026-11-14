namespace PriceTracker.Application.Interfaces.Repositories;

using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public interface IScrapeLogRepository
{
    Task<IEnumerable<ScrapeLog>> GetAllAsync();
    Task<IEnumerable<ScrapeLog>> GetByStoreIdAsync(Guid storeId);
    Task<IEnumerable<ScrapeLog>> GetByStatusAsync(ScrapeStatus status);
    Task<ScrapeLog?>             GetByIdAsync(long logId);
    Task                         AddAsync(ScrapeLog scrapeLog);
    Task                         UpdateAsync(ScrapeLog scrapeLog);
}