namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Listings;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PriceTracker.API.Extensions;

[ApiController]
[Route("v1/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;
    private readonly ApplicationDbContext _db;

    public ListingsController(IListingService listingService, ApplicationDbContext db)
    {
        _listingService = listingService;
        _db = db;
    }

    [HttpPatch("{listingId:guid}/exclude")]
    [Authorize]
    public async Task<IActionResult> ExcludeOffer(Guid listingId, CancellationToken ct)
        => await SetExcluded(listingId, true, ct);

    [HttpPatch("{listingId:guid}/include")]
    [Authorize]
    public async Task<IActionResult> IncludeOffer(Guid listingId, CancellationToken ct)
        => await SetExcluded(listingId, false, ct);

    private async Task<IActionResult> SetExcluded(Guid listingId, bool excluded, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var hasTracking = await _db.UserProductTrackings.AsNoTracking()
            .AnyAsync(t => t.UserId == userId && t.IsActive && (t.ListingId == listingId
                || t.ListingId == null && _db.StoreProductListings.Any(l => l.ListingId == listingId
                    && l.ProductId == t.ProductId)), ct);
        if (!hasTracking) return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "No active tracking covers this offer."));
        if (!await _db.StoreProductListings.AnyAsync(x => x.ListingId == listingId && x.IsActive, ct))
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Active offer not found."));

        await _db.StoreProductListings.Where(x => x.ListingId == listingId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsExcluded, excluded), ct);
        return Ok(ApiResponse<object>.Ok(new { listingId, isExcluded = excluded }));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetListings([FromQuery] string? productId, [FromQuery] Guid? storeId)
    {
        if (!string.IsNullOrWhiteSpace(productId))
        {
            if (Guid.TryParse(productId, out var guid))
            {
                var result = await _listingService.GetByProductIdAsync(guid);
                return Ok(ApiResponse<IEnumerable<ListingResponse>>.Ok(result));
            }
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "productId must be a product identifier."));
        }

        if (storeId.HasValue)
        {
            var result = await _listingService.GetByStoreIdAsync(storeId.Value);
            return Ok(ApiResponse<IEnumerable<ListingResponse>>.Ok(result));
        }

        return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "Either productId or storeId must be provided."));
    }

    [HttpGet("{listingId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid listingId)
    {
        var result = await _listingService.GetByIdAsync(listingId);
        return Ok(ApiResponse<ListingResponse>.Ok(result));
    }

    [HttpGet("by-product/{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var result = await _listingService.GetByProductIdAsync(productId);
        return Ok(ApiResponse<IEnumerable<ListingResponse>>.Ok(result));
    }

    [HttpGet("by-store/{storeId:guid}")]
    [AllowAnonymous]
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
