using NUnit.Framework;
using OAS.Contracts.Accounting.BankAccounts;

namespace OAS.Tests.Accounting.Application.CashBankAccounts;

[TestFixture]
public sealed class BankIbanValidationTests
{
    [Test]
    public void ValidIbanPassesModulo97()
    {
        Assert.That(BankIban.IsValid("GB82 WEST 1234 5698 7654 32"), Is.True);
        Assert.That(BankIban.Normalize("gb82 west 1234 5698 7654 32"), Is.EqualTo("GB82WEST12345698765432"));
    }

    [Test]
    public void InvalidIbanFailsModulo97()
    {
        Assert.That(BankIban.IsValid("GB83WEST12345698765432"), Is.False);
        Assert.That(BankIban.IsValid("AA00INVALID"), Is.False);
    }

    [Test]
    public void EmptyIbanIsAllowed()
    {
        Assert.That(BankIban.IsValid(null), Is.True);
        Assert.That(BankIban.IsValid("  "), Is.True);
        Assert.That(BankIban.Normalize("  "), Is.Null);
    }
}
