namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Common;
using Dealio.Domain.Entities;
using Dealio.Domain.Enums;

public interface IScrapeLogService
{
    Task<PagedResult<ScrapeLog>> GetAllAsync(PaginationRequest pagination);
    Task<PagedResult<ScrapeLog>> GetByStoreIdAsync(Guid storeId, PaginationRequest pagination);
    Task<PagedResult<ScrapeLog>> GetByStatusAsync(ScrapeStatus status, PaginationRequest pagination);
    Task<ScrapeLog?>             GetByIdAsync(long logId);
    Task<ScrapeLog>              CreateAsync(ScrapeLog scrapeLog);
    Task                         UpdateAsync(ScrapeLog scrapeLog);
}