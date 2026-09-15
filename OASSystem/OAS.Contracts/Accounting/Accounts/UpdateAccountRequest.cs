using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Accounts;

public sealed record UpdateAccountRequest(
    string Code,
    string NameAr,
    string? NameEn,
    Guid? ParentAccountId,
    byte Level,
    AccountClass AccountClass,
    AccountType AccountType,
    NormalBalance NormalBalance,
    bool IsPostingAccount,
    bool IsControlAccount,
    bool AllowManualPosting,
    bool IsSystemAccount,
    bool IsActive,
    DateOnly? EffectiveDate,
    string RowVersion);