using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Inventory.Products;
using DomainLensDetails = OAS.Domain.Entities.Inventory.LensDetails;

namespace OAS.Application.Inventory.Products.LensDetails;

public sealed class LensDetailsMapper
    : ICrudMapper<DomainLensDetails, Guid, LensDetailsDto, CreateLensDetailsRequest, UpdateLensDetailsRequest>
{
    public DomainLensDetails Create(CreateLensDetailsRequest source) =>
        new(
            source.ProductVariantId,
            source.LensType,
            source.IsPrescriptionLens,
            source.Material,
            source.Coating,
            source.RefractiveIndex,
            source.SPH,
            source.CYL,
            source.ADD,
            source.BaseCurve,
            source.Diameter,
            source.IsActive);

    public void Update(UpdateLensDetailsRequest source, DomainLensDetails destination) =>
        destination.UpdateDetails(
            source.LensType,
            source.IsPrescriptionLens,
            source.Material,
            source.Coating,
            source.RefractiveIndex,
            source.SPH,
            source.CYL,
            source.ADD,
            source.BaseCurve,
            source.Diameter,
            source.IsActive);

    public LensDetailsDto ToRead(DomainLensDetails source) =>
        new(
            source.Id,
            source.ProductVariantId,
            source.LensType,
            source.Material,
            source.Coating,
            source.RefractiveIndex,
            source.SPH,
            source.CYL,
            source.ADD,
            source.BaseCurve,
            source.Diameter,
            source.IsPrescriptionLens,
            source.IsActive);
}
