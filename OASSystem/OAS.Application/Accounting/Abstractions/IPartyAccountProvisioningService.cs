using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.Abstractions;
public interface IPartyAccountProvisioningService
{
    Task<Account> ProvisionCustomerAccountAsync(Guid parentAccountId,string nameAr,string? nameEn,bool isActive,DateOnly? effectiveDate,CancellationToken cancellationToken=default);
    Task<Account> ProvisionSupplierAccountAsync(Guid parentAccountId,string nameAr,string? nameEn,bool isActive,DateOnly? effectiveDate,CancellationToken cancellationToken=default);
    Task SynchronizeAsync(Guid accountId,string nameAr,string? nameEn,bool? isActive,CancellationToken cancellationToken=default);
}
