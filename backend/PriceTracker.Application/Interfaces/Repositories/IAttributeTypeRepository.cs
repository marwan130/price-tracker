namespace PriceTracker.Application.Interfaces.Repositories;

using PriceTracker.Domain.Entities;

public interface IAttributeTypeRepository
{
    Task<IEnumerable<AttributeType>> GetAllAsync();
    Task<AttributeType?>             GetByIdAsync(long attributeTypeId);
    Task<bool>                       ExistsByNameAsync(string name);
    Task                             AddAsync(AttributeType attributeType);
    Task                             UpdateAsync(AttributeType attributeType);
    Task                             DeleteAsync(AttributeType attributeType);

    Task<IEnumerable<AttributeValue>> GetValuesByTypeIdAsync(long attributeTypeId);
    Task<AttributeValue?>            GetValueByIdAsync(long attributeValueId);
    Task<bool>                       ExistsValueAsync(long attributeTypeId, string value);
    Task                             AddValueAsync(AttributeValue attributeValue);
    Task                             UpdateValueAsync(AttributeValue attributeValue);
    Task                             DeleteValueAsync(AttributeValue attributeValue);
}