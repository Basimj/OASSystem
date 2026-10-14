using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.CoreHardening.Services;

public interface IAccountIntegrityService
{
    Task ValidateHierarchyAsync(
        Guid accountId,
        Guid? parentAccountId,
        byte level,
        CancellationToken cancellationToken = default);

    Task ValidateUpdateAsync(
        Account current,
        Guid? parentAccountId,
        byte level,
        AccountClass accountClass,
        AccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        bool isControlAccount,
        bool isSystemAccount,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task ValidateDeactivationAsync(
        Account account,
        CancellationToken cancellationToken = default);
}
