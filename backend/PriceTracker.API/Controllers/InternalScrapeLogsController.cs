namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.ScrapeLogs;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

[ApiController]
[Route("v1/scrape-logs")]
public class InternalScrapeLogsController : ControllerBase
{
    private readonly IScrapeLogService _scrapeLogService;

    public InternalScrapeLogsController(IScrapeLogService scrapeLogService)
        => _scrapeLogService = scrapeLogService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScrapeLogRequest request)
    {
        if (!Enum.TryParse<ScrapeStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", $"Invalid scrape status '{request.Status}'."));

        var scrapeLog = new ScrapeLog
        {
            StoreId      = request.StoreId,
            ListingId    = request.ListingId,
            Status       = status,
            ErrorMessage = request.ErrorMessage,
            ItemsScraped = request.ItemsScraped,
            StartedAt    = request.StartedAt,
            FinishedAt   = request.FinishedAt
        };

        var result = await _scrapeLogService.CreateAsync(scrapeLog);
        return Ok(ApiResponse<ScrapeLog>.Ok(result, "Scrape log created successfully."));
    }
}
