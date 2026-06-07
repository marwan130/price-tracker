namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Stores;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/stores")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoresController(IStoreService storeService)
        => _storeService = storeService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _storeService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<StoreResponse>>.Ok(result));
    }

    [HttpGet("{storeId:guid}")]
    public async Task<IActionResult> GetById(Guid storeId)
    {
        var result = await _storeService.GetByIdAsync(storeId);
        return Ok(ApiResponse<StoreResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request)
    {
        var result = await _storeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { storeId = result.StoreId },
            ApiResponse<StoreResponse>.Ok(result, "Store created successfully."));
    }

    [HttpPut("{storeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid storeId, [FromBody] UpdateStoreRequest request)
    {
        var result = await _storeService.UpdateAsync(storeId, request);
        return Ok(ApiResponse<StoreResponse>.Ok(result, "Store updated successfully."));
    }

    [HttpDelete("{storeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid storeId)
    {
        await _storeService.DeleteAsync(storeId);
        return Ok(ApiResponse<object>.Ok(null!, "Store deleted successfully."));
    }
}