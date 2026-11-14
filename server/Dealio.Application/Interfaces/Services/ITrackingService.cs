namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Tracking;

public interface ITrackingService
{
    Task<IEnumerable<TrackingResponse>> GetByUserIdAsync(Guid userId);
    Task<TrackingResponse>              GetByIdAsync(Guid trackingId, Guid userId);
    Task<TrackingResponse>              CreateAsync(Guid userId, CreateTrackingRequest request);
    Task<TrackingResponse>              UpdateAsync(Guid trackingId, Guid userId, UpdateTrackingRequest request);
    Task                                DeleteAsync(Guid trackingId, Guid userId);
    Task                                ActivateAsync(Guid trackingId, Guid userId);
    Task                                DeactivateAsync(Guid trackingId, Guid userId);
}