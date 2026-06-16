namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Attributes;

public interface IAttributeTypeService
{
    Task<IEnumerable<AttributeTypeResponse>> GetAllAsync();
    Task<AttributeTypeResponse>              GetByIdAsync(long attributeTypeId);
    Task<AttributeTypeResponse>              CreateAsync(CreateAttributeTypeRequest request);
    Task<AttributeTypeResponse>              UpdateAsync(long attributeTypeId, UpdateAttributeTypeRequest request);
    Task                                     DeleteAsync(long attributeTypeId);

    Task<IEnumerable<AttributeValueResponse>> GetValuesByTypeIdAsync(long attributeTypeId);
    Task<AttributeValueResponse>              GetValueByIdAsync(long attributeValueId);
    Task<AttributeValueResponse>              CreateValueAsync(long attributeTypeId, CreateAttributeValueRequest request);
    Task<AttributeValueResponse>              UpdateValueAsync(long attributeValueId, UpdateAttributeValueRequest request);
    Task                                      DeleteValueAsync(long attributeValueId);
}