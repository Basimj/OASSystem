using NUnit.Framework;
using OAS.Domain.Accounting.Entities;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class PostingProfileDomainTests
{
    [Test]
    public void Create_ValidPostingProfile_InitializesProperties()
    {
        var id = Guid.NewGuid();
        var profile = PostingProfile.Create(
            id,
            "PP-SALES-INV",
            "ملف ترحيل فواتير المبيعات",
            "Sales",
            "SalesInvoice",
            isActive: true);

        Assert.That(profile.Id, Is.EqualTo(id));
        Assert.That(profile.Code, Is.EqualTo("PP-SALES-INV"));
        Assert.That(profile.Name, Is.EqualTo("ملف ترحيل فواتير المبيعات"));
        Assert.That(profile.Module, Is.EqualTo("Sales"));
        Assert.That(profile.DocumentType, Is.EqualTo("SalesInvoice"));
        Assert.That(profile.IsActive, Is.True);
    }

    [Test]
    public void UpdateDetails_UpdatesFieldsCorrectly()
    {
        var profile = PostingProfile.Create(
            Guid.NewGuid(), "PP-01", "ملف 1", "Sales", "Invoice", true);

        profile.UpdateDetails("PP-02", "ملف مبيعات معدل", "Sales", "SalesInvoice");

        Assert.That(profile.Code, Is.EqualTo("PP-02"));
        Assert.That(profile.Name, Is.EqualTo("ملف مبيعات معدل"));
    }

    [Test]
    public void SetActive_TogglesActiveFlag()
    {
        var profile = PostingProfile.Create(
            Guid.NewGuid(), "PP-01", "ملف 1", "Sales", "Invoice", true);

        profile.SetActive(false);
        Assert.That(profile.IsActive, Is.False);
    }
}
