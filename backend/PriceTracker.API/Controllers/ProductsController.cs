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

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetById(Guid productId)
    {
        var result = await _productService.GetByIdAsync(productId);
        return Ok(ApiResponse<ProductResponse>.Ok(result));
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