namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetByListing(
        Guid                          listingId,
        [FromQuery] PriceHistoryFilterRequest filter,
        [FromQuery] PaginationRequest         pagination)
    {
        var result = await _priceHistoryService.GetByListingIdAsync(listingId, filter, pagination);
        return Ok(ApiResponse<PagedResult<PriceRecordResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _priceHistoryService.GetByIdAsync(id);
        return Ok(ApiResponse<PriceRecordResponse>.Ok(result));
    }

    [HttpGet("trend/{listingId:guid}")]
    public async Task<IActionResult> GetTrend(
        Guid                          listingId,
        [FromQuery] PriceHistoryFilterRequest filter)
    {
        var result = await _priceHistoryService.GetTrendAsync(listingId, filter);
        return Ok(ApiResponse<PriceTrendResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePriceRecordRequest request)
    {
        var result = await _priceHistoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<PriceRecordResponse>.Ok(result, "Price record created successfully."));
    }
}