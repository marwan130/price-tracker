namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Categories;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
        => _categoryService = categoryService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CategoryResponse>>.Ok(result));
    }

    [HttpGet("{categoryId:long}")]
    public async Task<IActionResult> GetById(long categoryId)
    {
        var result = await _categoryService.GetByIdAsync(categoryId);
        return Ok(ApiResponse<CategoryResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _categoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { categoryId = result.CategoryId },
            ApiResponse<CategoryResponse>.Ok(result, "Category created successfully."));
    }

    [HttpPut("{categoryId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(long categoryId, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _categoryService.UpdateAsync(categoryId, request);
        return Ok(ApiResponse<CategoryResponse>.Ok(result, "Category updated successfully."));
    }

    [HttpDelete("{categoryId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(long categoryId)
    {
        await _categoryService.DeleteAsync(categoryId);
        return Ok(ApiResponse<object>.Ok(null!, "Category deleted successfully."));
    }
}