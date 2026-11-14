namespace Dealio.Application.Interfaces.Repositories;

using Dealio.Domain.Entities;

public interface ITrackingRepository
{
    Task<IEnumerable<UserProductTracking>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<UserProductTracking>> GetActiveTrackingsAsync();
    Task<UserProductTracking?>             GetByIdAsync(Guid trackingId);
    Task<bool>                             ExistsAsync(Guid userId, Guid productId, Guid? listingId);
    Task                                   AddAsync(UserProductTracking tracking);
    Task                                   UpdateAsync(UserProductTracking tracking);
    Task                                   DeleteAsync(UserProductTracking tracking);
}
