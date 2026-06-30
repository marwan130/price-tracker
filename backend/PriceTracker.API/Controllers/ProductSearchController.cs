namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/products/search")]
[Authorize]
[EnableRateLimiting("search")]
public class ProductSearchController : ControllerBase
{
    private readonly IProductSearchService _productSearchService;

    public ProductSearchController(IProductSearchService productSearchService)
        => _productSearchService = productSearchService;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<IEnumerable<ProductSearchResult>>.Fail("VALIDATION_ERROR", "Query is required"));

        var results = await _productSearchService.SearchProductsAsync(query, ct);
        return Ok(ApiResponse<IEnumerable<ProductSearchResult>>.Ok(results));
    }

    [HttpPost("url")]
    public async Task<IActionResult> SearchByUrl([FromBody] SearchByUrlRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(ApiResponse<ProductSearchResult?>.Fail("VALIDATION_ERROR", "URL is required"));

        var result = await _productSearchService.SearchByUrlAsync(request.Url, ct);
        
        if (result == null)
            return NotFound(ApiResponse<ProductSearchResult?>.Fail("NOT_FOUND", "Product not found"));

        return Ok(ApiResponse<ProductSearchResult>.Ok(result));
    }
}
