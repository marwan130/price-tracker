namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Variants;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1")]
public class VariantsController : ControllerBase
{
    private readonly IProductVariantService _variantService;

    public VariantsController(IProductVariantService variantService)
        => _variantService = variantService;

    [HttpGet("products/{productId:guid}/variants")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var result = await _variantService.GetByProductIdAsync(productId);
        return Ok(ApiResponse<IEnumerable<VariantResponse>>.Ok(result));
    }

    [HttpGet("variants/{variantId:guid}")]
    public async Task<IActionResult> GetById(Guid variantId)
    {
        var result = await _variantService.GetByIdAsync(variantId);
        return Ok(ApiResponse<VariantResponse>.Ok(result));
    }

    [HttpPost("products/{productId:guid}/variants")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateVariantRequest request)
    {
        var result = await _variantService.CreateAsync(productId, request);
        return CreatedAtAction(nameof(GetById), new { variantId = result.VariantId },
            ApiResponse<VariantResponse>.Ok(result, "Variant created successfully."));
    }

    [HttpPut("variants/{variantId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid variantId, [FromBody] UpdateVariantRequest request)
    {
        var result = await _variantService.UpdateAsync(variantId, request);
        return Ok(ApiResponse<VariantResponse>.Ok(result, "Variant updated successfully."));
    }

    [HttpDelete("variants/{variantId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid variantId)
    {
        await _variantService.DeleteAsync(variantId);
        return Ok(ApiResponse<object>.Ok(null!, "Variant deleted successfully."));
    }
}