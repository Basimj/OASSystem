using OAS.Contracts.Accounting.Enums;

namespace OAS.Client.Accounting.Common;

public readonly record struct AccountingVoucherSettlementSelection(
    Guid? CashAccountId,
    Guid? BankAccountId,
    Guid? SettlementAccountId);

/// <summary>
/// Keeps the user's settlement choice separate from the internal GL settlement snapshot.
/// Cash uses CashAccountId only, bank/card/cheque use BankAccountId only, and Other uses SettlementAccountId only.
/// </summary>
public static class AccountingVoucherSettlementSelectionRules
{
    public static AccountingVoucherSettlementSelection Normalize(
        PaymentMethod? paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        Guid? settlementAccountId) => paymentMethod switch
    {
        PaymentMethod.Cash => new(cashAccountId, null, null),
        PaymentMethod.BankTransfer or PaymentMethod.Card or PaymentMethod.Cheque =>
            new(null, bankAccountId, null),
        PaymentMethod.Other or null => new(null, null, settlementAccountId),
        _ => new(null, null, settlementAccountId)
    };
}
