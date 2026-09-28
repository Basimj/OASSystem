using NUnit.Framework;
using OAS.Client.Accounting.Common;
using OAS.Contracts.Accounting.Enums;

namespace OAS.Tests.Accounting.Client;

[TestFixture]
public sealed class AccountingVoucherSettlementSelectionRulesTests
{
    [Test]
    public void Cash_KeepsOnlyCashAccount()
    {
        var cash = Guid.NewGuid();
        var bank = Guid.NewGuid();
        var internalSettlement = Guid.NewGuid();

        var result = AccountingVoucherSettlementSelectionRules.Normalize(
            PaymentMethod.Cash, cash, bank, internalSettlement);

        Assert.Multiple(() =>
        {
            Assert.That(result.CashAccountId, Is.EqualTo(cash));
            Assert.That(result.BankAccountId, Is.Null);
            Assert.That(result.SettlementAccountId, Is.Null);
        });
    }

    [TestCase(PaymentMethod.BankTransfer)]
    [TestCase(PaymentMethod.Card)]
    [TestCase(PaymentMethod.Cheque)]
    public void BankMethods_KeepOnlyBankAccount(PaymentMethod method)
    {
        var cash = Guid.NewGuid();
        var bank = Guid.NewGuid();
        var internalSettlement = Guid.NewGuid();

        var result = AccountingVoucherSettlementSelectionRules.Normalize(
            method, cash, bank, internalSettlement);

        Assert.Multiple(() =>
        {
            Assert.That(result.CashAccountId, Is.Null);
            Assert.That(result.BankAccountId, Is.EqualTo(bank));
            Assert.That(result.SettlementAccountId, Is.Null);
        });
    }

    [Test]
    public void Other_KeepsOnlyExplicitSettlementAccount()
    {
        var cash = Guid.NewGuid();
        var bank = Guid.NewGuid();
        var settlement = Guid.NewGuid();

        var result = AccountingVoucherSettlementSelectionRules.Normalize(
            PaymentMethod.Other, cash, bank, settlement);

        Assert.Multiple(() =>
        {
            Assert.That(result.CashAccountId, Is.Null);
            Assert.That(result.BankAccountId, Is.Null);
            Assert.That(result.SettlementAccountId, Is.EqualTo(settlement));
        });
    }

    [Test]
    public void LegacyUnknownMethod_DoesNotTreatInternalSettlementAsCashOrBankSelection()
    {
        var settlement = Guid.NewGuid();

        var result = AccountingVoucherSettlementSelectionRules.Normalize(
            null, Guid.NewGuid(), Guid.NewGuid(), settlement);

        Assert.Multiple(() =>
        {
            Assert.That(result.CashAccountId, Is.Null);
            Assert.That(result.BankAccountId, Is.Null);
            Assert.That(result.SettlementAccountId, Is.EqualTo(settlement));
        });
    }
}
