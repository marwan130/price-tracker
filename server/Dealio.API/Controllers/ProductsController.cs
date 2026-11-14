namespace Dealio.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.Products;
using Dealio.Application.Interfaces.Services;
using Dealio.Infrastructure.Persistence;

[ApiController]
[Route("v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ApplicationDbContext _db;
    private readonly IProductImageResolver _imageResolver;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ProductsController(IProductService productService, ApplicationDbContext db, IProductImageResolver imageResolver)
    {
        _productService = productService;
        _db = db;
        _imageResolver = imageResolver;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] ProductFilterRequest filter,
        [FromQuery] PaginationRequest    pagination)
    {
        var result = await _productService.GetAllAsync(filter, pagination);
        return Ok(ApiResponse<PagedResult<ProductSummaryResponse>>.Ok(result));
    }

    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid productId)
    {
        var result = await _productService.GetByIdAsync(productId);
        return Ok(ApiResponse<ProductResponse>.Ok(result));
    }

    // Photos are not persisted anywhere; this resolves one live from the
    // retailer page tracked products (or an arbitrary watched PDP) point at.
    [HttpGet("image-resolve")]
    [AllowAnonymous]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> ResolveImage([FromQuery] Guid? productId, [FromQuery] string? url, CancellationToken ct)
    {
        // Exactly one selector per call keeps the cache keys unambiguous.
        if (productId is null && string.IsNullOrWhiteSpace(url)) 
            return BadRequest(ApiResponse<ImageResolveResponse>.Fail("VALIDATION_ERROR", "Provide productId or url."));
        if (productId is not null && !string.IsNullOrWhiteSpace(url))
            return BadRequest(ApiResponse<ImageResolveResponse>.Fail("VALIDATION_ERROR", "Provide exactly one of productId or url."));

        if (productId is not { } id)
        {
            // Explicit URLs skip nothing: the same https + product-page gates
            // the tracked-listing path applies before any retailer fetch.
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps)
                return BadRequest(ApiResponse<ImageResolveResponse>.Fail("VALIDATION_ERROR", "Enter a valid HTTPS product URL."));
            return await Resolve(() => _imageResolver.ResolveForUrlAsync(parsed, ct));
        }

        return await Resolve(() => _imageResolver.ResolveForProductAsync(id, ct));

        async Task<IActionResult> Resolve(Func<Task<string?>> resolve)
        {
            try
            {
                var image = await resolve();
                return Ok(ApiResponse<ImageResolveResponse>.Ok(new ImageResolveResponse { ImageUrl = image }));
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<ImageResolveResponse>.Fail("IMAGE_RESOLVE_TIMEOUT", "The retailer page took too long to respond. Prices and stock remain available."));
            }
        }
    }

    [HttpPost("import-url")]
    public async Task<IActionResult> ImportUrl([FromBody] SearchByUrlRequest request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return BadRequest(ApiResponse<ProductResponse>.Fail("VALIDATION_ERROR", "Enter a valid HTTPS product URL."));
        }

        var now = DateTime.UtcNow;
        var recentSessionIds = await _db.ProductSearchSessions.AsNoTracking()
            .Where(session => (session.Status == "completed" || session.Status == "awaiting_verification") && session.ExpiresAtUtc > now
                && session.CreatedAtUtc > now.AddHours(-1))
            .OrderByDescending(session => session.CreatedAtUtc)
            .Take(100)
            .Select(session => session.SessionId)
            .ToListAsync(ct);
        var candidatePayloads = await _db.ProductSearchSessionEvents.AsNoTracking()
            .Where(item => recentSessionIds.Contains(item.SessionId) && item.EventType == "candidate")
            .OrderByDescending(item => item.OccurredAtUtc)
            .Select(item => item.Payload)
            .ToListAsync(ct);
        var candidate = candidatePayloads
            .Select(payload => JsonSerializer.Deserialize<ProductSearchResult>(payload, JsonOptions))
            .FirstOrDefault(candidate => candidate is { Price: > 0 }
                && !string.IsNullOrWhiteSpace(candidate.Name)
                && !string.IsNullOrWhiteSpace(candidate.Currency)
                && Uri.TryCreate(candidate.ProductUrl, UriKind.Absolute, out var candidateUri)
                && string.Equals(NormalizeProductUrl(candidateUri), NormalizeProductUrl(uri), StringComparison.OrdinalIgnoreCase));
        if (candidate is null)
            return Conflict(ApiResponse<object>.Fail("SEARCH_RESULT_REQUIRED", "Search for this product before adding it to your collection."));

        var result = await _productService.ImportSearchResultAsync(candidate);
        return Ok(ApiResponse<ProductResponse>.Ok(result, "Product added to your collection."));
    }

    private static string NormalizeProductUrl(Uri uri) => uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var result = await _productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { productId = result.ProductId },
            ApiResponse<ProductResponse>.Ok(result, "Product created successfully."));
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateProductRequest request)
    {
        var result = await _productService.UpdateAsync(productId, request);
        return Ok(ApiResponse<ProductResponse>.Ok(result, "Product updated successfully."));
    }

    [HttpDelete("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid productId)
    {
        await _productService.DeleteAsync(productId);
        return Ok(ApiResponse<object>.Ok(null!, "Product deleted successfully."));
    }
}
