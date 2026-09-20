using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class AccountDomainTests
{
    [Test]
    public void Create_ValidParameters_CreatesAccountWithCorrectProperties()
    {
        var id = Guid.NewGuid();
        var code = "1101";
        var nameAr = "الصندوق الرئيسي";
        var nameEn = "Main Cash Box";
        var parentId = Guid.NewGuid();
        byte level = 3;

        var account = Account.Create(
            id,
            code,
            nameAr,
            nameEn,
            parentId,
            level,
            AccountClass.Asset,
            AccountType.Posting,
            NormalBalance.Debit,
            isPostingAccount: true,
            isControlAccount: false,
            allowManualPosting: true,
            isSystemAccount: false,
            isActive: true,
            effectiveDate: new DateOnly(2026, 1, 1));

        Assert.That(account.Id, Is.EqualTo(id));
        Assert.That(account.Code, Is.EqualTo("1101"));
        Assert.That(account.NameAr, Is.EqualTo("الصندوق الرئيسي"));
        Assert.That(account.NameEn, Is.EqualTo("Main Cash Box"));
        Assert.That(account.ParentAccountId, Is.EqualTo(parentId));
        Assert.That(account.Level, Is.EqualTo(3));
        Assert.That(account.AccountClass, Is.EqualTo(AccountClass.Asset));
        Assert.That(account.AccountType, Is.EqualTo(AccountType.Posting));
        Assert.That(account.NormalBalance, Is.EqualTo(NormalBalance.Debit));
        Assert.That(account.IsPostingAccount, Is.True);
        Assert.That(account.AllowManualPosting, Is.True);
        Assert.That(account.IsActive, Is.True);
    }

    [Test]
    public void Create_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Account.Create(
                Guid.Empty,
                "101",
                "الأصول",
                null,
                null,
                1,
                AccountClass.Asset,
                AccountType.Header,
                NormalBalance.Debit,
                false, false, false, true, true, null));
    }

    [Test]
    public void Create_EmptyCodeOrNameAr_ThrowsArgumentException()
    {
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            Account.Create(
                id,
                "",
                "الأصول",
                null,
                null,
                1,
                AccountClass.Asset,
                AccountType.Header,
                NormalBalance.Debit,
                false, false, false, true, true, null));

        Assert.Throws<ArgumentException>(() =>
            Account.Create(
                id,
                "101",
                "",
                null,
                null,
                1,
                AccountClass.Asset,
                AccountType.Header,
                NormalBalance.Debit,
                false, false, false, true, true, null));
    }

    [Test]
    public void Create_ZeroLevel_ThrowsArgumentOutOfRangeException()
    {
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Account.Create(
                id,
                "101",
                "الأصول",
                null,
                null,
                0,
                AccountClass.Asset,
                AccountType.Header,
                NormalBalance.Debit,
                false, false, false, true, true, null));
    }

    [Test]
    public void UpdateDetails_ValidValues_UpdatesAccountCorrectly()
    {
        var account = Account.Create(
            Guid.NewGuid(), "1101", "الصندوق", null, null, 1,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            true, false, true, false, true, null);

        var newParent = Guid.NewGuid();
        account.UpdateDetails(
            "1102", "الصندوق الفرعي", "Branch Cash", newParent, 2,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            true, true, true, new DateOnly(2026, 2, 1));

        Assert.That(account.Code, Is.EqualTo("1102"));
        Assert.That(account.NameAr, Is.EqualTo("الصندوق الفرعي"));
        Assert.That(account.NameEn, Is.EqualTo("Branch Cash"));
        Assert.That(account.ParentAccountId, Is.EqualTo(newParent));
        Assert.That(account.Level, Is.EqualTo(2));
        Assert.That(account.IsControlAccount, Is.True);
    }

    [Test]
    public void SetActive_ChangesIsActiveState()
    {
        var account = Account.Create(
            Guid.NewGuid(), "1101", "الصندوق", null, null, 1,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            true, false, true, false, true, null);

        account.SetActive(false);
        Assert.That(account.IsActive, Is.False);

        account.SetActive(true);
        Assert.That(account.IsActive, Is.True);
    }

    [Test]
    public void CanReceivePosting_RulesEnforcedCorrectly()
    {
        var activePostingAccount = Account.Create(
            Guid.NewGuid(), "1101", "الصندوق", null, null, 1,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: true,
            isSystemAccount: false, isActive: true, effectiveDate: null);

        Assert.That(activePostingAccount.CanReceivePosting(isManual: true), Is.True);
        Assert.That(activePostingAccount.CanReceivePosting(isManual: false), Is.True);

        var headerAccount = Account.Create(
            Guid.NewGuid(), "1000", "الأصول المتداولة", null, null, 1,
            AccountClass.Asset, AccountType.Header, NormalBalance.Debit,
            isPostingAccount: false, isControlAccount: false, allowManualPosting: false,
            isSystemAccount: false, isActive: true, effectiveDate: null);

        Assert.That(headerAccount.CanReceivePosting(), Is.False);

        var inactiveAccount = Account.Create(
            Guid.NewGuid(), "1102", "حساب مجمد", null, null, 1,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: true,
            isSystemAccount: false, isActive: false, effectiveDate: null);

        Assert.That(inactiveAccount.CanReceivePosting(), Is.False);

        var systemAccountDisallowingManual = Account.Create(
            Guid.NewGuid(), "1103", "حساب نظامي", null, null, 1,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: false,
            isSystemAccount: true, isActive: true, effectiveDate: null);

        Assert.That(systemAccountDisallowingManual.CanReceivePosting(isManual: true), Is.False);
        Assert.That(systemAccountDisallowingManual.CanReceivePosting(isManual: false), Is.True);
    }
}
