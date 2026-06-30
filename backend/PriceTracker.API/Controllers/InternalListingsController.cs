namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PriceTracker.Application.DTOs.Internal;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/internal/listings")]
[AllowAnonymous]
public class InternalListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    public InternalListingsController(IListingService listingService)
        => _listingService = listingService;

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] int page = 0, [FromQuery] int size = 100)
    {
        var result = await _listingService.GetActiveForScrapingAsync(page, size);
        return Ok(ApiResponse<IEnumerable<ScrapeListingResponse>>.Ok(result));
    }
}