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

    [HttpGet]
    public async Task<IActionResult> GetListings([FromQuery] string? productId, [FromQuery] Guid? storeId)
    {
        if (!string.IsNullOrWhiteSpace(productId))
        {
            if (Guid.TryParse(productId, out var guid))
            {
                var result = await _listingService.GetByProductIdAsync(guid);
                return Ok(ApiResponse<IEnumerable<ListingResponse>>.Ok(result));
            }
            else
            {
                var url = Uri.UnescapeDataString(productId);
                if (url.StartsWith("https:/") && !url.StartsWith("https://"))
                {
                    url = "https://" + url[7..];
                }
                else if (url.StartsWith("http:/") && !url.StartsWith("http://"))
                {
                    url = "http://" + url[6..];
                }
                var result = await _listingService.GetByProductUrlAsync(url);
                return Ok(ApiResponse<IEnumerable<ListingResponse>>.Ok(result));
            }
        }

        if (storeId.HasValue)
        {
            var result = await _listingService.GetByStoreIdAsync(storeId.Value);
            return Ok(ApiResponse<IEnumerable<ListingResponse>>.Ok(result));
        }

        return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "Either productId or storeId must be provided."));
    }

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