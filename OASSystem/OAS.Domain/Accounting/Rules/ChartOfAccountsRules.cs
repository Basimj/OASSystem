using OAS.Domain.Accounting.Enums;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Rules;

public static class ChartOfAccountsRules
{
    public static void EnsureClassificationIsValid(AccountClass accountClass, NormalBalance normalBalance)
    {
        if (!Enum.IsDefined(accountClass))
            throw new DomainException("Account class is invalid.");
        if (!Enum.IsDefined(normalBalance))
            throw new DomainException("Account normal balance is invalid.");
    }

    public static void EnsureDefinitionIsConsistent(
        AccountType accountType,
        bool isPostingAccount,
        bool isControlAccount,
        bool allowManualPosting)
    {
        if (!Enum.IsDefined(accountType))
            throw new DomainException("Account type is invalid.");

        if (accountType == AccountType.Header && isPostingAccount)
            throw new DomainException("Header accounts cannot receive posting.");

        if (accountType == AccountType.Header && allowManualPosting)
            throw new DomainException("Header accounts cannot allow manual posting.");

        if (allowManualPosting && !isPostingAccount)
            throw new DomainException("Manual posting can only be enabled for posting-capable accounts.");

        if (accountType == AccountType.Control && !isControlAccount)
            throw new DomainException("Control account type must be marked as a control account.");

    }

    public static void EnsureParentIsNotSelf(Guid accountId, Guid? parentAccountId)
    {
        if (parentAccountId.HasValue && parentAccountId.Value == accountId)
            throw new DomainException("An account cannot be its own parent.");
    }

    public static void EnsureLevelMatchesParent(byte level, byte? parentLevel)
    {
        if (!parentLevel.HasValue)
        {
            if (level != 1)
                throw new DomainException("A root account must have level 1.");
            return;
        }

        if (parentLevel.Value == byte.MaxValue)
            throw new DomainException("The parent account cannot have another child level.");

        if (level != parentLevel.Value + 1)
            throw new DomainException("Account level must be exactly one level below its parent.");
    }
}
