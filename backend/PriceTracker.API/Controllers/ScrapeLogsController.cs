namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.ScrapeLogs;
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
        return Ok(ApiResponse<PagedResult<ScrapeLogResponse>>.Ok(MapPaged(result)));
    }

    [HttpGet("{logId:long}")]
    public async Task<IActionResult> GetById(long logId)
    {
        var result = await _scrapeLogService.GetByIdAsync(logId);
        return Ok(ApiResponse<ScrapeLogResponse?>.Ok(result is null ? null : MapToResponse(result)));
    }

    [HttpGet("by-store/{storeId:guid}")]
    public async Task<IActionResult> GetByStore(
        Guid storeId,
        [FromQuery] PaginationRequest pagination)
    {
        var result = await _scrapeLogService.GetByStoreIdAsync(storeId, pagination);
        return Ok(ApiResponse<PagedResult<ScrapeLogResponse>>.Ok(MapPaged(result)));
    }

    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(
        ScrapeStatus status,
        [FromQuery] PaginationRequest pagination)
    {
        var result = await _scrapeLogService.GetByStatusAsync(status, pagination);
        return Ok(ApiResponse<PagedResult<ScrapeLogResponse>>.Ok(MapPaged(result)));
    }

    private static PagedResult<ScrapeLogResponse> MapPaged(PagedResult<ScrapeLog> result)
        => PagedResult<ScrapeLogResponse>.From(
            result.Content.Select(MapToResponse),
            result.Page,
            result.Size,
            result.TotalElements);

    private static ScrapeLogResponse MapToResponse(ScrapeLog log)
        => new()
        {
            LogId = log.LogId,
            StoreId = log.StoreId,
            StoreName = log.Store?.Name ?? string.Empty,
            ListingId = log.ListingId,
            Status = log.Status.ToString(),
            ErrorMessage = log.ErrorMessage,
            ItemsScraped = log.ItemsScraped,
            StartedAt = log.StartedAt,
            FinishedAt = log.FinishedAt
        };
}