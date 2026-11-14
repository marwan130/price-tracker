namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.PriceHistory;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/price-history")]
public class PriceHistoryController : ControllerBase
{
    private readonly IPriceHistoryService _priceHistoryService;

    public PriceHistoryController(IPriceHistoryService priceHistoryService)
        => _priceHistoryService = priceHistoryService;

    [HttpGet("by-listing/{listingId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByListing(
        Guid                          listingId,
        [FromQuery] PriceHistoryFilterRequest filter,
        [FromQuery] PaginationRequest         pagination)
    {
        var result = await _priceHistoryService.GetByListingIdAsync(listingId, filter, pagination);
        return Ok(ApiResponse<PagedResult<PriceRecordResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _priceHistoryService.GetByIdAsync(id);
        return Ok(ApiResponse<PriceRecordResponse>.Ok(result));
    }

    [HttpGet("trend/{listingId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTrend(
        Guid                          listingId,
        [FromQuery] PriceHistoryFilterRequest filter)
    {
        var result = await _priceHistoryService.GetTrendAsync(listingId, filter);
        return Ok(ApiResponse<PriceTrendResponse>.Ok(result));
    }

    [HttpGet("drops/recent")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRecentDrops([FromQuery] int size = 5)
    {
        var result = await _priceHistoryService.GetRecentDropsAsync(size);
        return Ok(ApiResponse<IReadOnlyList<RecentPriceDropResponse>>.Ok(result));
    }

}
