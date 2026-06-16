namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs.Attributes;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.Interfaces.Services;

[ApiController]
[Route("v1/attribute-types")]
public class AttributeTypesController : ControllerBase
{
    private readonly IAttributeTypeService _attributeTypeService;

    public AttributeTypesController(IAttributeTypeService attributeTypeService)
        => _attributeTypeService = attributeTypeService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _attributeTypeService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<AttributeTypeResponse>>.Ok(result));
    }

    [HttpGet("{typeId:long}")]
    public async Task<IActionResult> GetById(long typeId)
    {
        var result = await _attributeTypeService.GetByIdAsync(typeId);
        return Ok(ApiResponse<AttributeTypeResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAttributeTypeRequest request)
    {
        var result = await _attributeTypeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { typeId = result.AttributeTypeId },
            ApiResponse<AttributeTypeResponse>.Ok(result, "Attribute type created successfully."));
    }

    [HttpPut("{typeId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(long typeId, [FromBody] UpdateAttributeTypeRequest request)
    {
        var result = await _attributeTypeService.UpdateAsync(typeId, request);
        return Ok(ApiResponse<AttributeTypeResponse>.Ok(result, "Attribute type updated successfully."));
    }

    [HttpDelete("{typeId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(long typeId)
    {
        await _attributeTypeService.DeleteAsync(typeId);
        return Ok(ApiResponse<object>.Ok(null!, "Attribute type deleted successfully."));
    }

    [HttpGet("{typeId:long}/values")]
    public async Task<IActionResult> GetValues(long typeId)
    {
        var result = await _attributeTypeService.GetValuesByTypeIdAsync(typeId);
        return Ok(ApiResponse<IEnumerable<AttributeValueResponse>>.Ok(result));
    }

    [HttpGet("{typeId:long}/values/{valueId:long}")]
    public async Task<IActionResult> GetValueById(long typeId, long valueId)
    {
        var result = await _attributeTypeService.GetValueByIdAsync(valueId);
        if (result.AttributeTypeId != typeId)
        {
            return BadRequest(ApiResponse<object>.Fail("bad_request", "Value does not belong to this attribute type."));
        }
        return Ok(ApiResponse<AttributeValueResponse>.Ok(result));
    }

    [HttpPost("{typeId:long}/values")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateValue(long typeId, [FromBody] CreateAttributeValueRequest request)
    {
        var result = await _attributeTypeService.CreateValueAsync(typeId, request);
        return CreatedAtAction(nameof(GetValueById), new { typeId = typeId, valueId = result.AttributeValueId },
            ApiResponse<AttributeValueResponse>.Ok(result, "Attribute value created successfully."));
    }

    [HttpPut("{typeId:long}/values/{valueId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateValue(long typeId, long valueId, [FromBody] UpdateAttributeValueRequest request)
    {
        var val = await _attributeTypeService.GetValueByIdAsync(valueId);
        if (val.AttributeTypeId != typeId)
        {
            return BadRequest(ApiResponse<object>.Fail("bad_request", "Value does not belong to this attribute type."));
        }

        var result = await _attributeTypeService.UpdateValueAsync(valueId, request);
        return Ok(ApiResponse<AttributeValueResponse>.Ok(result, "Attribute value updated successfully."));
    }

    [HttpDelete("{typeId:long}/values/{valueId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteValue(long typeId, long valueId)
    {
        var val = await _attributeTypeService.GetValueByIdAsync(valueId);
        if (val.AttributeTypeId != typeId)
        {
            return BadRequest(ApiResponse<object>.Fail("bad_request", "Value does not belong to this attribute type."));
        }

        await _attributeTypeService.DeleteValueAsync(valueId);
        return Ok(ApiResponse<object>.Ok(null!, "Attribute value deleted successfully."));
    }
}