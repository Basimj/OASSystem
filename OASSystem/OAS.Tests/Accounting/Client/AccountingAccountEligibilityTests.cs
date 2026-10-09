using NUnit.Framework;
using OAS.Client.Accounting.Common;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.Enums;

namespace OAS.Tests.Accounting.Client;

[TestFixture]
public sealed class AccountingAccountEligibilityTests
{
    [Test]
    public void ManualJournal_InactiveAccount_IsDisabledWithReason()
    {
        var account = CreateAccount(isActive: false);

        var result = AccountingAccountEligibility.Evaluate(
            account,
            AccountingAccountEligibilityContext.ManualJournal);
        var lookup = AccountingLookupEligibility.ToAccountLookup(
            account,
            AccountingAccountEligibilityContext.ManualJournal);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsEligible, Is.False);
            Assert.That(result.ReasonCode, Is.EqualTo("account_inactive"));
            Assert.That(lookup.Disabled, Is.True);
            Assert.That(lookup.SecondaryText, Does.Contain("غير نشط"));
        });
    }

    [Test]
    public void ManualJournal_HeaderAccount_IsInvalid()
    {
        var account = CreateAccount(
            accountType: AccountType.Header,
            isPostingAccount: false);

        var result = AccountingAccountEligibility.Evaluate(
            account,
            AccountingAccountEligibilityContext.ManualJournal);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsEligible, Is.False);
            Assert.That(result.ReasonCode, Is.EqualTo("account_header"));
        });
    }

    [Test]
    public void ManualJournal_PostingAccountWithManualPermission_IsValid()
    {
        var account = CreateAccount(allowManualPosting: true);

        var result = AccountingAccountEligibility.Evaluate(
            account,
            AccountingAccountEligibilityContext.ManualJournal);

        Assert.That(result.IsEligible, Is.True);
    }

    [Test]
    public void AccountWithoutManualPermission_IsInvalidForManualButValidForAutomaticPosting()
    {
        var account = CreateAccount(allowManualPosting: false);

        var manual = AccountingAccountEligibility.Evaluate(
            account,
            AccountingAccountEligibilityContext.ManualJournal);
        var automatic = AccountingAccountEligibility.Evaluate(
            account,
            AccountingAccountEligibilityContext.AutomaticPosting);

        Assert.Multiple(() =>
        {
            Assert.That(manual.IsEligible, Is.False);
            Assert.That(manual.ReasonCode, Is.EqualTo("manual_posting_not_allowed"));
            Assert.That(automatic.IsEligible, Is.True);
        });
    }

    [Test]
    public void AssetControlParent_RequiresActiveAssetDebitControlAccount()
    {
        var valid = CreateAccount(
            accountClass: AccountClass.Asset,
            accountType: AccountType.Control,
            normalBalance: NormalBalance.Debit,
            isPostingAccount: false,
            isControlAccount: true,
            allowManualPosting: false);

        var invalid = CreateAccount(
            accountClass: AccountClass.Liability,
            accountType: AccountType.Control,
            normalBalance: NormalBalance.Credit,
            isPostingAccount: false,
            isControlAccount: true,
            allowManualPosting: false);

        Assert.Multiple(() =>
        {
            Assert.That(AccountingAccountEligibility.Evaluate(
                valid,
                AccountingAccountEligibilityContext.AssetControlParent).IsEligible, Is.True);

            var invalidResult = AccountingAccountEligibility.Evaluate(
                invalid,
                AccountingAccountEligibilityContext.AssetControlParent);
            Assert.That(invalidResult.IsEligible, Is.False);
            Assert.That(invalidResult.ReasonCode, Is.EqualTo("parent_wrong_class"));
        });
    }


    [Test]
    public void CustomerAdvances_RequiresActivePostingLiabilityCreditAccount()
    {
        var valid = CreateAccount(
            accountClass: AccountClass.Liability,
            accountType: AccountType.Posting,
            normalBalance: NormalBalance.Credit,
            isPostingAccount: true);

        var wrongClass = CreateAccount(
            accountClass: AccountClass.Asset,
            accountType: AccountType.Posting,
            normalBalance: NormalBalance.Debit,
            isPostingAccount: true);

        var invalid = AccountingAccountEligibility.Evaluate(
            wrongClass,
            AccountingAccountEligibilityContext.CustomerAdvances);

        Assert.Multiple(() =>
        {
            Assert.That(AccountingAccountEligibility.Evaluate(
                valid,
                AccountingAccountEligibilityContext.CustomerAdvances).IsEligible, Is.True);
            Assert.That(invalid.IsEligible, Is.False);
            Assert.That(invalid.ReasonCode, Is.EqualTo("customer_advances_wrong_class"));
        });
    }

    [Test]
    public void LiabilityControlParent_RequiresActiveLiabilityCreditControlAccount()
    {
        var valid = CreateAccount(
            accountClass: AccountClass.Liability,
            accountType: AccountType.Control,
            normalBalance: NormalBalance.Credit,
            isPostingAccount: false,
            isControlAccount: true,
            allowManualPosting: false);

        var invalid = CreateAccount(
            accountClass: AccountClass.Asset,
            accountType: AccountType.Control,
            normalBalance: NormalBalance.Debit,
            isPostingAccount: false,
            isControlAccount: true,
            allowManualPosting: false);

        Assert.Multiple(() =>
        {
            Assert.That(AccountingAccountEligibility.Evaluate(
                valid,
                AccountingAccountEligibilityContext.LiabilityControlParent).IsEligible, Is.True);

            var invalidResult = AccountingAccountEligibility.Evaluate(
                invalid,
                AccountingAccountEligibilityContext.LiabilityControlParent);
            Assert.That(invalidResult.IsEligible, Is.False);
            Assert.That(invalidResult.ReasonCode, Is.EqualTo("parent_wrong_class"));
        });
    }

    private static AccountDto CreateAccount(
        AccountClass accountClass = AccountClass.Asset,
        AccountType accountType = AccountType.Posting,
        NormalBalance normalBalance = NormalBalance.Debit,
        bool isPostingAccount = true,
        bool isControlAccount = false,
        bool allowManualPosting = true,
        bool isActive = true) =>
        new(
            Guid.NewGuid(),
            "110101",
            "حساب اختباري",
            null,
            null,
            1,
            accountClass,
            accountType,
            normalBalance,
            isPostingAccount,
            isControlAccount,
            allowManualPosting,
            IsSystemAccount: false,
            IsActive: isActive,
            EffectiveDate: null,
            RowVersion: string.Empty);
}
