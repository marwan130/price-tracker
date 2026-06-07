namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Users;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
        => _userService = userService;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

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
        await _userService.DeactivateAsync(CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null!, "Account deactivated successfully."));
    }
}