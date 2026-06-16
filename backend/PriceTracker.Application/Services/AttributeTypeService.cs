namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Attributes;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class AttributeTypeService : IAttributeTypeService
{
    private readonly IAttributeTypeRepository _repository;

    public AttributeTypeService(IAttributeTypeRepository repository)
        => _repository = repository;

    public async Task<IEnumerable<AttributeTypeResponse>> GetAllAsync()
    {
        var types = await _repository.GetAllAsync();
        return types.Select(MapToResponse);
    }

    public async Task<AttributeTypeResponse> GetByIdAsync(long attributeTypeId)
    {
        var type = await _repository.GetByIdAsync(attributeTypeId)
            ?? throw new NotFoundException(nameof(AttributeType), attributeTypeId);
        return MapToResponse(type);
    }

    public async Task<AttributeTypeResponse> CreateAsync(CreateAttributeTypeRequest request)
    {
        if (await _repository.ExistsByNameAsync(request.Name))
            throw new ConflictException($"Attribute type '{request.Name}' already exists.");

        var type = new AttributeType { Name = request.Name };
        await _repository.AddAsync(type);
        return MapToResponse(type);
    }

    public async Task<AttributeTypeResponse> UpdateAsync(long attributeTypeId, UpdateAttributeTypeRequest request)
    {
        var type = await _repository.GetByIdAsync(attributeTypeId)
            ?? throw new NotFoundException(nameof(AttributeType), attributeTypeId);

        if (type.Name != request.Name && await _repository.ExistsByNameAsync(request.Name))
            throw new ConflictException($"Attribute type '{request.Name}' already exists.");

        type.Name = request.Name;
        await _repository.UpdateAsync(type);
        return MapToResponse(type);
    }

    public async Task DeleteAsync(long attributeTypeId)
    {
        var type = await _repository.GetByIdAsync(attributeTypeId)
            ?? throw new NotFoundException(nameof(AttributeType), attributeTypeId);

        await _repository.DeleteAsync(type);
    }

    public async Task<IEnumerable<AttributeValueResponse>> GetValuesByTypeIdAsync(long attributeTypeId)
    {
        var type = await _repository.GetByIdAsync(attributeTypeId)
            ?? throw new NotFoundException(nameof(AttributeType), attributeTypeId);

        var values = await _repository.GetValuesByTypeIdAsync(attributeTypeId);
        return values.Select(MapValueToResponse);
    }

    public async Task<AttributeValueResponse> GetValueByIdAsync(long attributeValueId)
    {
        var value = await _repository.GetValueByIdAsync(attributeValueId)
            ?? throw new NotFoundException(nameof(AttributeValue), attributeValueId);
        return MapValueToResponse(value);
    }

    public async Task<AttributeValueResponse> CreateValueAsync(long attributeTypeId, CreateAttributeValueRequest request)
    {
        var type = await _repository.GetByIdAsync(attributeTypeId)
            ?? throw new NotFoundException(nameof(AttributeType), attributeTypeId);

        if (await _repository.ExistsValueAsync(attributeTypeId, request.Value))
            throw new ConflictException($"Attribute value '{request.Value}' already exists for type '{type.Name}'.");

        var value = new AttributeValue
        {
            AttributeTypeId = attributeTypeId,
            Value = request.Value
        };
        await _repository.AddValueAsync(value);
        
        var reloaded = await _repository.GetValueByIdAsync(value.AttributeValueId);
        return MapValueToResponse(reloaded ?? value);
    }

    public async Task<AttributeValueResponse> UpdateValueAsync(long attributeValueId, UpdateAttributeValueRequest request)
    {
        var value = await _repository.GetValueByIdAsync(attributeValueId)
            ?? throw new NotFoundException(nameof(AttributeValue), attributeValueId);

        if (value.Value != request.Value && await _repository.ExistsValueAsync(value.AttributeTypeId, request.Value))
            throw new ConflictException($"Attribute value '{request.Value}' already exists for this type.");

        value.Value = request.Value;
        await _repository.UpdateValueAsync(value);
        return MapValueToResponse(value);
    }

    public async Task DeleteValueAsync(long attributeValueId)
    {
        var value = await _repository.GetValueByIdAsync(attributeValueId)
            ?? throw new NotFoundException(nameof(AttributeValue), attributeValueId);

        await _repository.DeleteValueAsync(value);
    }

    private static AttributeTypeResponse MapToResponse(AttributeType type) => new()
    {
        AttributeTypeId = type.AttributeTypeId,
        Name = type.Name,
        Values = type.Values.Select(MapValueToResponse).ToList()
    };

    private static AttributeValueResponse MapValueToResponse(AttributeValue value) => new()
    {
        AttributeValueId = value.AttributeValueId,
        AttributeTypeId = value.AttributeTypeId,
        Type = value.AttributeType?.Name ?? string.Empty,
        Value = value.Value
    };
}