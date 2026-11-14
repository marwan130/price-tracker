namespace Dealio.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dealio.API.Extensions;
using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.Users;
using Dealio.Application.Interfaces.Services;

[ApiController]
[Route("v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
        => _userService = userService;

    private Guid CurrentUserId => User.GetUserId();

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _userService.GetByIdAsync(CurrentUserId);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(CurrentUserId, request);
        return Ok(ApiResponse<UserResponse>.Ok(result, "Profile updated successfully."));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeactivateMe()
    {
        var deletionAt = await _userService.RequestDeletionAsync(CurrentUserId);
        return Ok(ApiResponse<object>.Ok(new { deletionScheduledAtUtc = deletionAt }, "Account deactivated. You can cancel deletion during the 30-day grace period."));
    }

    [HttpPost("me/deletion/cancel")]
    public async Task<IActionResult> CancelDeletion()
    {
        await _userService.CancelDeletionAsync(CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null!, "Account deletion cancelled."));
    }

    [HttpPatch("me/tracking/pause")]
    public async Task<IActionResult> PauseTracking()
    {
        await _userService.SetTrackingPausedAsync(CurrentUserId, true);
        return Ok(ApiResponse<object>.Ok(new { isTrackingPaused = true }, "All tracking paused."));
    }

    [HttpPatch("me/tracking/resume")]
    public async Task<IActionResult> ResumeTracking()
    {
        await _userService.SetTrackingPausedAsync(CurrentUserId, false);
        return Ok(ApiResponse<object>.Ok(new { isTrackingPaused = false }, "All tracking resumed."));
    }
}
