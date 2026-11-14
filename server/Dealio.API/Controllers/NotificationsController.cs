namespace Dealio.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dealio.API.Extensions;
using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.Notifications;
using Dealio.Application.Interfaces.Services;

[ApiController]
[Route("v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
        => _notificationService = notificationService;

    private Guid CurrentUserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination)
    {
        var result = await _notificationService.GetByUserIdAsync(CurrentUserId, pagination);
        return Ok(ApiResponse<PagedResult<NotificationResponse>>.Ok(result));
    }

    [HttpGet("{notificationId:long}")]
    public async Task<IActionResult> GetById(long notificationId)
    {
        var result = await _notificationService.GetByIdAsync(notificationId, CurrentUserId);
        return Ok(ApiResponse<NotificationResponse>.Ok(result));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(CurrentUserId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null!, "All notifications marked as read."));
    }

    [HttpPatch("{notificationId:long}/read")]
    public async Task<IActionResult> MarkAsRead(long notificationId)
    {
        await _notificationService.MarkAsReadAsync(notificationId, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null!, "Notification marked as read."));
    }
}
