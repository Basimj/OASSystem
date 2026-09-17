using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Mapping;

public sealed class SupplierAccountMapper
    : ICrudMapper<
        SupplierAccount,
        Guid,
        SupplierAccountDto,
        CreateSupplierAccountRequest,
        UpdateSupplierAccountRequest>
{
    public SupplierAccount Create(CreateSupplierAccountRequest source)
    {
        return SupplierAccount.Create(
            Guid.NewGuid(),
            source.SupplierId,
            source.AccountId,
            source.ControlAccountId,
            source.IsActive,
            DateTime.UtcNow);
    }

    public void Update(UpdateSupplierAccountRequest source, SupplierAccount destination)
    {
        destination.UpdateAccounts(
            source.AccountId,
            source.ControlAccountId);
    }

    public SupplierAccountDto ToRead(SupplierAccount source)
    {
        return new SupplierAccountDto(
            source.Id,
            source.SupplierId,
            source.AccountId,
            source.ControlAccountId,
            source.IsActive,
            source.CreatedAtUtc,
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
    }
}
