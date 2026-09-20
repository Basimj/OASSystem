using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using DomainLensDetails = OAS.Domain.Entities.Inventory.LensDetails;

namespace OAS.Application.Inventory.Products.LensDetails;

public sealed class LensDetailsMapper
    : ICrudMapper<DomainLensDetails, Guid, LensDetailsDto, CreateLensDetailsRequest, UpdateLensDetailsRequest>
{
    public DomainLensDetails Create(CreateLensDetailsRequest source)
    {
        return new DomainLensDetails(
            source.ProductId,
            source.LensType,
            source.IsPrescriptionLens,
            source.Material,
            source.Coating,
            source.RefractiveIndex,
            source.SphereMin,
            source.SphereMax,
            source.CylinderMin,
            source.CylinderMax,
            source.AddMin,
            source.AddMax);
    }

    public void Update(UpdateLensDetailsRequest source, DomainLensDetails destination)
    {
        destination.UpdateDetails(
            source.LensType,
            source.IsPrescriptionLens,
            source.Material,
            source.Coating,
            source.RefractiveIndex,
            source.SphereMin,
            source.SphereMax,
            source.CylinderMin,
            source.CylinderMax,
            source.AddMin,
            source.AddMax);
    }

    public LensDetailsDto ToRead(DomainLensDetails source)
    {
        return new LensDetailsDto(
            source.Id,
            source.ProductId,
            source.LensType,
            source.Material,
            source.Coating,
            source.RefractiveIndex,
            source.SphereMin,
            source.SphereMax,
            source.CylinderMin,
            source.CylinderMax,
            source.AddMin,
            source.AddMax,
            source.IsPrescriptionLens);
    }
}
