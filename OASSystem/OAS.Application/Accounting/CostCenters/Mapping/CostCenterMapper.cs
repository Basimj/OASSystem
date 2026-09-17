using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Mapping;

public sealed class CostCenterMapper
    : ICrudMapper<
        CostCenter,
        Guid,
        CostCenterDto,
        CreateCostCenterRequest,
        UpdateCostCenterRequest>
{
    public CostCenter Create(CreateCostCenterRequest source)
    {
        return CostCenter.Create(
            Guid.NewGuid(),
            source.Code,
            source.NameAr,
            source.NameEn,
            source.ParentCostCenterId,
            source.IsActive);
    }

    public void Update(UpdateCostCenterRequest source, CostCenter destination)
    {
        destination.UpdateDetails(
            source.Code,
            source.NameAr,
            source.NameEn,
            source.ParentCostCenterId);

        destination.SetActive(source.IsActive);
    }

    public CostCenterDto ToRead(CostCenter source)
    {
        return new CostCenterDto(
            source.Id,
            source.Code,
            source.NameAr,
            source.NameEn,
            source.ParentCostCenterId,
            source.IsActive,
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
    }
}
