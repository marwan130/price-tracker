namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Listings;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    public ListingsController(IListingService listingService)
        => _listingService = listingService;

    [HttpGet("{listingId:guid}")]
    public async Task<IActionResult> GetById(Guid listingId)
    {
        var result = await _listingService.GetByIdAsync(listingId);
        return Ok(ApiResponse<ListingResponse>.Ok(result));
    }

    [HttpGet("by-product/{productId:guid}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var result = await _listingService.GetByProductIdAsync(productId);
        return Ok(ApiResponse<IEnumerable<ListingResponse>>.Ok(result));
    }

    [HttpGet("by-store/{storeId:guid}")]
    public async Task<IActionResult> GetByStore(Guid storeId)
    {
        var result = await _listingService.GetByStoreIdAsync(storeId);
        return Ok(ApiResponse<IEnumerable<ListingResponse>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest request)
    {
        var result = await _listingService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { listingId = result.ListingId },
            ApiResponse<ListingResponse>.Ok(result, "Listing created successfully."));
    }

    [HttpPut("{listingId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid listingId, [FromBody] UpdateListingRequest request)
    {
        var result = await _listingService.UpdateAsync(listingId, request);
        return Ok(ApiResponse<ListingResponse>.Ok(result, "Listing updated successfully."));
    }

    [HttpDelete("{listingId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid listingId)
    {
        await _listingService.DeleteAsync(listingId);
        return Ok(ApiResponse<object>.Ok(null!, "Listing deleted successfully."));
    }
}