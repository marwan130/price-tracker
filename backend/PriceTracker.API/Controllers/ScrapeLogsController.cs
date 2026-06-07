namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

[ApiController]
[Route("v1/scrape-logs")]
[Authorize(Roles = "Admin")]
public class ScrapeLogsController : ControllerBase
{
    private readonly IScrapeLogService _scrapeLogService;

    public ScrapeLogsController(IScrapeLogService scrapeLogService)
        => _scrapeLogService = scrapeLogService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination)
    {
        var result = await _scrapeLogService.GetAllAsync(pagination);
        return Ok(ApiResponse<PagedResult<ScrapeLog>>.Ok(result));
    }

    [HttpGet("{logId:long}")]
    public async Task<IActionResult> GetById(long logId)
    {
        var result = await _scrapeLogService.GetByIdAsync(logId);
        return Ok(ApiResponse<ScrapeLog?>.Ok(result));
    }

    [HttpGet("by-store/{storeId:guid}")]
    public async Task<IActionResult> GetByStore(
        Guid storeId,
        [FromQuery] PaginationRequest pagination)
    {
        var result = await _scrapeLogService.GetByStoreIdAsync(storeId, pagination);
        return Ok(ApiResponse<PagedResult<ScrapeLog>>.Ok(result));
    }

    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(
        ScrapeStatus status,
        [FromQuery] PaginationRequest pagination)
    {
        var result = await _scrapeLogService.GetByStatusAsync(status, pagination);
        return Ok(ApiResponse<PagedResult<ScrapeLog>>.Ok(result));
    }
}