using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Abstractions;

public interface ILinkedAccountingAccountProvisioningService
{
    Task<Account> ProvisionCashAccountAsync(string nameAr, bool isActive, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default);
    Task<Account> ProvisionBankAccountAsync(string nameAr, bool isActive, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default);
    Task<Account> ProvisionEmployeeAccountAsync(string nameAr, bool isActive, DateOnly? effectiveDate = null, CancellationToken cancellationToken = default);
    Task SynchronizeAsync(Guid accountId, string nameAr, bool isActive, CancellationToken cancellationToken = default);
}
