using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.Rules;

namespace OAS.Application.Accounting.CoreHardening.Services;

public sealed class AccountIntegrityService(
    IReadRepository<Account, Guid> accounts,
    IReadRepository<JournalEntryLine, Guid> journalLines,
    IReadRepository<PostingProfileLine, Guid> postingProfileLines,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<EmployeeAccount, Guid> employeeAccounts,
    IReadRepository<CashAccount, Guid> cashAccounts,
    IReadRepository<BankAccount, Guid> bankAccounts,
    IReadRepository<ExpenseType, Guid> expenseTypes,
    IReadRepository<AccountingSettings, Guid> settings) : IAccountIntegrityService
{
    public async Task ValidateHierarchyAsync(
        Guid accountId,
        Guid? parentAccountId,
        byte level,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ChartOfAccountsRules.EnsureParentIsNotSelf(accountId, parentAccountId);
        }
        catch (Exception ex)
        {
            throw new ConflictException("accounting_account_self_parent", ex.Message);
        }

        if (!parentAccountId.HasValue)
        {
            try
            {
                ChartOfAccountsRules.EnsureLevelMatchesParent(level, null);
            }
            catch (Exception ex)
            {
                throw new ConflictException("accounting_account_root_level_invalid", ex.Message);
            }
            return;
        }

        var parent = await accounts.GetByIdAsync(parentAccountId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), parentAccountId.Value);

        if (!parent.IsActive)
        {
            throw new ConflictException(
                "accounting_account_parent_inactive",
                "Cannot assign an inactive account as parent.");
        }

        try
        {
            ChartOfAccountsRules.EnsureLevelMatchesParent(level, parent.Level);
        }
        catch (Exception ex)
        {
            throw new ConflictException("accounting_account_level_parent_mismatch", ex.Message);
        }

        var visited = new HashSet<Guid>();
        var cursor = parent;
        while (true)
        {
            if (cursor.Id == accountId)
            {
                throw new ConflictException(
                    "accounting_account_parent_cycle",
                    "The selected parent would create a cycle in the chart of accounts.");
            }

            if (!visited.Add(cursor.Id))
            {
                throw new ConflictException(
                    "accounting_account_existing_parent_cycle",
                    "The selected parent belongs to an invalid account hierarchy that already contains a cycle.");
            }

            if (!cursor.ParentAccountId.HasValue)
                break;

            cursor = await accounts.GetByIdAsync(cursor.ParentAccountId.Value, cancellationToken)
                ?? throw new ConflictException(
                    "accounting_account_parent_chain_broken",
                    "The selected parent has a broken parent hierarchy.");
        }
    }

    public async Task ValidateUpdateAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);

        await ValidateHierarchyAsync(current.Id, parentAccountId, level, cancellationToken);

        if (isSystemAccount != current.IsSystemAccount)
        {
            throw new ConflictException(
                "accounting_system_account_flag_immutable",
                "The system-account flag cannot be changed through account maintenance.");
        }

        var structuralChange =
            accountClass != current.AccountClass ||
            accountType != current.AccountType ||
            normalBalance != current.NormalBalance ||
            isPostingAccount != current.IsPostingAccount ||
            isControlAccount != current.IsControlAccount;

        if (structuralChange && await HasJournalHistoryAsync(current.Id, cancellationToken))
        {
            throw new ConflictException(
                "accounting_account_history_structure_immutable",
                "Account class, type, normal balance and posting/control nature cannot be changed after journal history exists.");
        }

        if (structuralChange && (current.IsSystemAccount || await IsMappedAsync(current.Id, cancellationToken)))
        {
            throw new ConflictException(
                "accounting_mapped_account_structure_immutable",
                "A system or mapped accounting account cannot be structurally changed until its mappings are safely replaced.");
        }

        if (!isActive && current.IsActive)
            await ValidateDeactivationAsync(current, cancellationToken);
    }

    public async Task ValidateDeactivationAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (account.IsSystemAccount)
        {
            throw new ConflictException(
                "accounting_system_account_deactivation_forbidden",
                "System accounts cannot be deactivated.");
        }

        if (await IsMappedAsync(account.Id, cancellationToken))
        {
            throw new ConflictException(
                "accounting_mapped_account_deactivation_forbidden",
                "The account is used by an accounting mapping and cannot be deactivated until the mapping is replaced.");
        }

        if (await accounts.CountAsync(
                new Specification<Account>().Where(x => x.ParentAccountId == account.Id && x.IsActive),
                cancellationToken) > 0)
        {
            throw new ConflictException(
                "accounting_account_active_children_exist",
                "An account with active child accounts cannot be deactivated.");
        }
    }

    private Task<bool> HasJournalHistoryAsync(Guid accountId, CancellationToken ct) =>
        CountExistsAsync(journalLines, new Specification<JournalEntryLine>().Where(x => x.AccountId == accountId), ct);

    private async Task<bool> IsMappedAsync(Guid accountId, CancellationToken ct)
    {
        if (await CountExistsAsync(postingProfileLines,
                new Specification<PostingProfileLine>().Where(x => x.AccountId == accountId), ct)) return true;
        if (await CountExistsAsync(customers,
                new Specification<Customer>().Where(x => x.AccountId == accountId), ct)) return true;
        if (await CountExistsAsync(suppliers,
                new Specification<Supplier>().Where(x => x.AccountId == accountId), ct)) return true;
        if (await CountExistsAsync(employeeAccounts,
                new Specification<EmployeeAccount>().Where(x => x.AccountId == accountId && x.IsActive), ct)) return true;
        if (await CountExistsAsync(cashAccounts,
                new Specification<CashAccount>().Where(x => x.AccountId == accountId), ct)) return true;
        if (await CountExistsAsync(bankAccounts,
                new Specification<BankAccount>().Where(x => x.AccountId == accountId), ct)) return true;
        if (await CountExistsAsync(expenseTypes,
                new Specification<ExpenseType>().Where(x => x.DefaultExpenseAccountId == accountId), ct)) return true;

        var accountingSettings = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct);
        if (accountingSettings is null) return false;

        return accountingSettings.EmployeeParentAccountId == accountId ||
               accountingSettings.CashParentAccountId == accountId ||
               accountingSettings.BankParentAccountId == accountId ||
               accountingSettings.ExchangeGainAccountId == accountId ||
               accountingSettings.ExchangeLossAccountId == accountId ||
               accountingSettings.RetainedEarningsAccountId == accountId ||
               accountingSettings.InventoryAccountId == accountId ||
               accountingSettings.GrniAccountId == accountId ||
               accountingSettings.PurchaseTaxAccountId == accountId ||
               accountingSettings.PurchasePriceVarianceAccountId == accountId;
    }

    private static async Task<bool> CountExistsAsync<TEntity>(
        IReadRepository<TEntity, Guid> repository,
        ISpecification<TEntity> specification,
        CancellationToken ct)
        where TEntity : OAS.Domain.Common.Entities.Entity<Guid> =>
        await repository.CountAsync(specification, ct) > 0;
}
