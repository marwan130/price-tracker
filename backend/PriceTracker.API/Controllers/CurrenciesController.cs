namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Currencies;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/currencies")]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrenciesController(ICurrencyService currencyService)
        => _currencyService = currencyService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _currencyService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CurrencyResponse>>.Ok(result));
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _currencyService.GetByCodeAsync(code);
        return Ok(ApiResponse<CurrencyResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest request)
    {
        var result = await _currencyService.CreateAsync(request);
        return CreatedAtAction(nameof(GetByCode), new { code = result.Code },
            ApiResponse<CurrencyResponse>.Ok(result, "Currency created successfully."));
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string code)
    {
        await _currencyService.DeleteAsync(code);
        return Ok(ApiResponse<object>.Ok(null!, "Currency deleted successfully."));
    }
}