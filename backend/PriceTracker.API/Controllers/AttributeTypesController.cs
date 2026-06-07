namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Attributes;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Repositories;

[ApiController]
[Route("v1/attribute-types")]
public class AttributeTypesController : ControllerBase
{
    private readonly IProductVariantRepository _variantRepository;

    public AttributeTypesController(IProductVariantRepository variantRepository)
        => _variantRepository = variantRepository;

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(ApiResponse<object>.Ok(null!, "Use /v1/attribute-types/{typeId}/values to get values."));
    }
}