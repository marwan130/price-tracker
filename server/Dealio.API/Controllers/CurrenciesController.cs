namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Http;
using System.Text.RegularExpressions;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Currencies;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Markets;

[ApiController]
[Route("v1/currencies")]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;
    private readonly ICurrencyConversionService _currencyConversionService;

    public CurrenciesController(ICurrencyService currencyService, ICurrencyConversionService currencyConversionService)
    {
        _currencyService = currencyService;
        _currencyConversionService = currencyConversionService;
    }

    [HttpPost("convert")]
    [AllowAnonymous]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> Convert([FromBody] CurrencyConversionRequest request, CancellationToken ct)
    {
        if (!IsCurrencyCode(request.TargetCurrency) || request.Items is null || request.Items.Count is < 1 or > 100
            || request.Items.Any(item => item.Amount <= 0 || !IsCurrencyCode(item.CurrencyCode)))
            return BadRequest(ApiResponse<CurrencyConversionResponse>.Fail("VALIDATION_ERROR", "Provide 1–100 positive amounts with supported currency codes."));

        try
        {
            return Ok(ApiResponse<CurrencyConversionResponse>.Ok(await _currencyConversionService.ConvertAsync(request, ct)));
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<CurrencyConversionResponse>.Fail("RATES_UNAVAILABLE", "Live exchange rates are temporarily unavailable. Original prices remain available."));
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<CurrencyConversionResponse>.Fail("RATES_UNAVAILABLE", "The exchange-rate request timed out. Original prices remain available."));
        }
        catch (InvalidDataException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<CurrencyConversionResponse>.Fail("RATES_UNAVAILABLE", "The exchange-rate source returned unusable data. Original prices remain available."));
        }
    }

    [HttpGet("rates")]
    [AllowAnonymous]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> GetRates(CancellationToken ct)
    {
        try
        {
            return Ok(ApiResponse<CurrencyRatesResponse>.Ok(await _currencyConversionService.GetRateSnapshotAsync(ct)));
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<CurrencyRatesResponse>.Fail("RATES_UNAVAILABLE", "Live exchange rates are temporarily unavailable."));
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<CurrencyRatesResponse>.Fail("RATES_UNAVAILABLE", "The exchange-rate request timed out."));
        }
        catch (InvalidDataException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<CurrencyRatesResponse>.Fail("RATES_UNAVAILABLE", "The exchange-rate source returned unusable data."));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = (await _currencyService.GetAllAsync()).Where(item => IsCurrencyCode(item.Code));
        return Ok(ApiResponse<IEnumerable<CurrencyResponse>>.Ok(result));
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        if (!IsCurrencyCode(code))
            return NotFound(ApiResponse<CurrencyResponse>.Fail("CURRENCY_NOT_FOUND", "Currency is not supported."));
        var result = await _currencyService.GetByCodeAsync(code);
        return Ok(ApiResponse<CurrencyResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest request)
    {
        if (!IsCurrencyCode(request.Code))
            return BadRequest(ApiResponse<CurrencyResponse>.Fail("UNSUPPORTED_CURRENCY", "Choose a supported currency code."));
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

    private static bool IsCurrencyCode(string? code)
        => code is { Length: 3 } && MiddleEastMarkets.SupportedCurrencyCodes.Contains(code);
}
