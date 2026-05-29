namespace PriceTracker.Application.Interfaces.Repositories;

using PriceTracker.Domain.Entities;

public interface ITrackingRepository
{
    Task<IEnumerable<UserProductTracking>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<UserProductTracking>> GetActiveTrackingsAsync();
    Task<UserProductTracking?>             GetByIdAsync(Guid trackingId);
    Task<bool>                             ExistsAsync(Guid userId, Guid productId, Guid? variantId, Guid? listingId);
    Task                                   AddAsync(UserProductTracking tracking);
    Task                                   UpdateAsync(UserProductTracking tracking);
    Task                                   DeleteAsync(UserProductTracking tracking);
}