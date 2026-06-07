namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Tracking;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;

    public TrackingController(ITrackingService trackingService)
        => _trackingService = trackingService;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _trackingService.GetByUserIdAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<TrackingResponse>>.Ok(result));
    }

    [HttpGet("{trackingId:guid}")]
    public async Task<IActionResult> GetById(Guid trackingId)
    {
        var result = await _trackingService.GetByIdAsync(trackingId, CurrentUserId);
        return Ok(ApiResponse<TrackingResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTrackingRequest request)
    {
        var result = await _trackingService.CreateAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { trackingId = result.TrackingId },
            ApiResponse<TrackingResponse>.Ok(result, "Tracking added successfully."));
    }

    [HttpPut("{trackingId:guid}")]
    public async Task<IActionResult> Update(Guid trackingId, [FromBody] UpdateTrackingRequest request)
    {
        var result = await _trackingService.UpdateAsync(trackingId, CurrentUserId, request);
        return Ok(ApiResponse<TrackingResponse>.Ok(result, "Tracking updated successfully."));
    }

    [HttpDelete("{trackingId:guid}")]
    public async Task<IActionResult> Delete(Guid trackingId)
    {
        await _trackingService.DeleteAsync(trackingId, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null!, "Tracking removed successfully."));
    }

    [HttpPatch("{trackingId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid trackingId)
    {
        await _trackingService.ActivateAsync(trackingId, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null!, "Tracking activated."));
    }

    [HttpPatch("{trackingId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid trackingId)
    {
        await _trackingService.DeactivateAsync(trackingId, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null!, "Tracking deactivated."));
    }
}