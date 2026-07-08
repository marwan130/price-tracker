namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PriceTracker.Application.DTOs.Internal;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;

[ApiController]
[Route("v1/internal/listings")]
[AllowAnonymous]
public class InternalListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    public InternalListingsController(IListingService listingService)
    {
        _listingService = listingService;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(
        [FromQuery] int? categoryId = null,
        [FromQuery] Guid? storeId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 100)
    {
        IEnumerable<ScrapeListingResponse> result;
        
        if (categoryId.HasValue || storeId.HasValue || minPrice.HasValue || maxPrice.HasValue || !string.IsNullOrEmpty(currencyCode))
        {
            result = await _listingService.GetActiveForScrapingFilteredAsync(categoryId, storeId, minPrice, maxPrice, currencyCode, page, size);
        }
        else
        {
            result = await _listingService.GetActiveForScrapingAsync(page, size);
        }
        
        return Ok(ApiResponse<IEnumerable<ScrapeListingResponse>>.Ok(result));
    }
}