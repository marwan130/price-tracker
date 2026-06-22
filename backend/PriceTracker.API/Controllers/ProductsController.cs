namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
        => _productService = productService;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ProductFilterRequest filter,
        [FromQuery] PaginationRequest    pagination)
    {
        var result = await _productService.GetAllAsync(filter, pagination);
        return Ok(ApiResponse<PagedResult<ProductSummaryResponse>>.Ok(result));
    }

    [HttpGet("{*productId}")]
    public async Task<IActionResult> GetById(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return BadRequest(ApiResponse<ProductResponse>.Fail("VALIDATION_ERROR", "Product identifier is required."));
        }

        if (Guid.TryParse(productId, out var guid))
        {
            var result = await _productService.GetByIdAsync(guid);
            return Ok(ApiResponse<ProductResponse>.Ok(result));
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
            var result = await _productService.GetByUrlAsync(url);
            return Ok(ApiResponse<ProductResponse>.Ok(result));
        }
    }

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