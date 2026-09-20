using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using DomainFrameDetails = OAS.Domain.Entities.Inventory.FrameDetails;

namespace OAS.Application.Inventory.Products.FrameDetails;

public sealed class FrameDetailsMapper
    : ICrudMapper<DomainFrameDetails, Guid, FrameDetailsDto, CreateFrameDetailsRequest, UpdateFrameDetailsRequest>
{
    public DomainFrameDetails Create(CreateFrameDetailsRequest source)
    {
        return new DomainFrameDetails(
            source.ProductId,
            source.Model,
            source.Material,
            source.RimType,
            source.Gender,
            source.Shape,
            source.TempleLength,
            source.BridgeSize,
            source.LensWidth);
    }

    public void Update(UpdateFrameDetailsRequest source, DomainFrameDetails destination)
    {
        destination.UpdateDetails(
            source.Model,
            source.Material,
            source.RimType,
            source.Gender,
            source.Shape,
            source.TempleLength,
            source.BridgeSize,
            source.LensWidth);
    }

    public FrameDetailsDto ToRead(DomainFrameDetails source)
    {
        return new FrameDetailsDto(
            source.Id,
            source.ProductId,
            source.Model,
            source.Material,
            source.RimType,
            source.Gender,
            source.Shape,
            source.TempleLength,
            source.BridgeSize,
            source.LensWidth);
    }
}
