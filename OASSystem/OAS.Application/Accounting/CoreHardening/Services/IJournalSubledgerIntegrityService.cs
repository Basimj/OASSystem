using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CoreHardening.Services;

public interface IJournalSubledgerIntegrityService
{
    Task<string?> ValidateAndResolvePartyNameAsync(
        Account account,
        Guid? customerId,
        Guid? supplierId,
        Guid? employeeId,
        CancellationToken cancellationToken = default);
}
