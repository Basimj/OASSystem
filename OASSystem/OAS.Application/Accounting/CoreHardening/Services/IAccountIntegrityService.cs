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

    /// <summary>
    /// Read-only advisory validation of an attempted account update.
    /// Returns the first known business-rule code instead of throwing a conflict.
    /// Write commands always enforce their own rules again.
    /// </summary>
    Task<string?> InspectUpdateAsync(
        Account current,
        Guid? parentAccountId,
        byte level,
        AccountClass accountClass,
        AccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        bool isControlAccount,
        bool allowManualPosting,
        bool isSystemAccount,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task ValidateDeactivationAsync(
        Account account,
        CancellationToken cancellationToken = default);
}
