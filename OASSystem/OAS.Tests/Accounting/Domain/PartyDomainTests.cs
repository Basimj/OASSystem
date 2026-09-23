using NUnit.Framework;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.ValueObjects;
using OAS.Domain.Exceptions;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class PartyDomainTests
{
    private static PartyContactInfo Contact(ContactMethod method = ContactMethod.Mobile) =>
        PartyContactInfo.Create(null, null, null, "777000001", null, null, null, null, method, PartyAddress.Empty);

    [Test]
    public void Customer_Create_UsesIndependentOperationalAndAccountIdentity()
    {
        var accountId = Guid.NewGuid();
        var customer = Customer.Create(Guid.NewGuid(), "CUS-000125", accountId, PartyEntityType.Individual,
            "أحمد محمد", null, null, "123", null, null, null, Gender.Male, Contact(), true, 1000m, 30, null, true, null);

        Assert.That(customer.CustomerCode, Is.EqualTo("CUS-000125"));
        Assert.That(customer.AccountId, Is.EqualTo(accountId));
        Assert.That(customer.CreditLimit, Is.EqualTo(1000m));
        Assert.That(customer.PaymentTermDays, Is.EqualTo(30));
    }

    [Test]
    public void Customer_CashOnly_ZeroesCreditTerms()
    {
        var customer = Customer.Create(Guid.NewGuid(), "CUS-000001", Guid.NewGuid(), PartyEntityType.Individual,
            "عميل نقدي", null, null, null, null, null, null, Gender.Unspecified, Contact(), false, 900m, 45, null, true, null);

        Assert.That(customer.CreditLimit, Is.Zero);
        Assert.That(customer.PaymentTermDays, Is.Zero);
    }

    [Test]
    public void Supplier_InvalidScope_IsRejected()
    {
        Assert.Throws<DomainException>(() => Supplier.Create(Guid.NewGuid(), "SUP-000001", Guid.NewGuid(),
            PartyEntityType.Organization, SupplierScope.Unknown, "مورد", null, null, null, null, null,
            Contact(ContactMethod.Phone), 0m, 0, null, null, true, null));
    }

    [TestCase(1L, "CUS-000001")]
    [TestCase(125L, "CUS-000125")]
    public void CustomerCodeFormatter_FormatsSixDigits(long value, string expected) =>
        Assert.That(CustomerCodeFormatter.Format(value), Is.EqualTo(expected));

    [TestCase(1L, "SUP-000001")]
    [TestCase(84L, "SUP-000084")]
    public void SupplierCodeFormatter_FormatsSixDigits(long value, string expected) =>
        Assert.That(SupplierCodeFormatter.Format(value), Is.EqualTo(expected));
}
