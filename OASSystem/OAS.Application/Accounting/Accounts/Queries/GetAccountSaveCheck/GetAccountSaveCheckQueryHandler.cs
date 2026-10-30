using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Contracts.Accounting.Accounts;
using OAS.Domain.Accounting.Entities;
using DomainAccountClass = OAS.Domain.Accounting.Enums.AccountClass;
using DomainAccountType = OAS.Domain.Accounting.Enums.AccountType;
using DomainNormalBalance = OAS.Domain.Accounting.Enums.NormalBalance;

namespace OAS.Application.Accounting.Accounts.Queries.GetAccountSaveCheck;

/// <summary>
/// Advisory check with no writes and no deliberately thrown business conflicts.
/// The existing update handler still enforces every rule at write time.
/// </summary>
public sealed class GetAccountSaveCheckQueryHandler(
    IReadRepository<Account, Guid> accounts,
    IManagedAccountGuard managedAccounts,
    IAccountIntegrityService integrity)
    : IRequestHandler<GetAccountSaveCheckQuery, AccountSaveCheckDto>
{
    public async Task<AccountSaveCheckDto> Handle(
        GetAccountSaveCheckQuery request, CancellationToken cancellationToken)
    {
        var current = await accounts.GetByIdAsync(request.Id, cancellationToken);
        if (current is null)
            return Reject("accounting_account_not_found");

        var data = request.Data;
        if (data is null)
            return Reject("accounting_account_data_required");
        if (string.IsNullOrWhiteSpace(data.RowVersion))
            return Reject("accounting_account_row_version_invalid");

        byte[] version;
        try { version = Convert.FromBase64String(data.RowVersion); }
        catch (FormatException) { return Reject("accounting_account_row_version_invalid"); }
        if (!version.SequenceEqual(current.RowVersion))
            return Reject("accounting_account_stale_row_version");

        if (await managedAccounts.IsManagedAsync(current.Id, cancellationToken))
            return Reject("accounting_managed_account_edit_forbidden");

        if (string.IsNullOrWhiteSpace(data.Code) ||
            !string.Equals(current.Code, data.Code.Trim(), StringComparison.OrdinalIgnoreCase))
            return Reject("accounting_account_code_immutable");

        if (data.NameAr is null || string.IsNullOrWhiteSpace(data.NameAr))
            return Reject("accounting_account_name_required");
        if (data.Code.Length > 30 || data.NameAr.Length > 150 || data.NameEn?.Length > 150)
            return Reject("accounting_account_fields_too_long");
        if (data.Level == 0)
            return Reject("accounting_account_level_invalid");
        if (data.ParentAccountId == Guid.Empty)
            return Reject("accounting_account_parent_invalid");

        var issue = await integrity.InspectUpdateAsync(
            current,
            data.ParentAccountId,
            data.Level,
            (DomainAccountClass)data.AccountClass,
            (DomainAccountType)data.AccountType,
            (DomainNormalBalance)data.NormalBalance,
            data.IsPostingAccount,
            data.IsControlAccount,
            data.AllowManualPosting,
            data.IsSystemAccount,
            data.IsActive,
            cancellationToken);

        return issue is null ? new AccountSaveCheckDto(true, null) : Reject(issue);
    }

    private static AccountSaveCheckDto Reject(string code) => new(false, code);
}
