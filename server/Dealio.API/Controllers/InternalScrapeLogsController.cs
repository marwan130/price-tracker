namespace Dealio.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.ScrapeLogs;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Domain.Enums;

[ApiController]
[Route("v1/scrape-logs")]
public class InternalScrapeLogsController : ControllerBase
{
    private readonly IScrapeLogService _scrapeLogService;

    public InternalScrapeLogsController(IScrapeLogService scrapeLogService)
        => _scrapeLogService = scrapeLogService;

    [HttpPost]
    [AllowAnonymous]
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
