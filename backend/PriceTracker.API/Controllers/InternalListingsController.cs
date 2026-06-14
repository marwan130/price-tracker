namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Internal;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/internal/listings")]
public class InternalListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    public InternalListingsController(IListingService listingService)
        => _listingService = listingService;

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _listingService.GetActiveForScrapingAsync();
        return Ok(ApiResponse<IEnumerable<ScrapeListingResponse>>.Ok(result));
    }
}
