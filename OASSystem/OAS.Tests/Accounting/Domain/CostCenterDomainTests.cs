using NUnit.Framework;
using OAS.Domain.Accounting.Entities;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class CostCenterDomainTests
{
    [Test]
    public void Create_ValidCostCenter_InitializesProperties()
    {
        var id = Guid.NewGuid();
        var center = CostCenter.Create(
            id,
            "CC-01",
            "قسم المبيعات",
            "Sales Department",
            parentCostCenterId: null,
            isActive: true);

        Assert.That(center.Id, Is.EqualTo(id));
        Assert.That(center.Code, Is.EqualTo("CC-01"));
        Assert.That(center.NameAr, Is.EqualTo("قسم المبيعات"));
        Assert.That(center.NameEn, Is.EqualTo("Sales Department"));
        Assert.That(center.IsActive, Is.True);
    }

    [Test]
    public void UpdateDetails_UpdatesPropertiesCorrectly()
    {
        var center = CostCenter.Create(
            Guid.NewGuid(), "CC-01", "المبيعات", null, null, true);

        var parentId = Guid.NewGuid();
        center.UpdateDetails("CC-01-01", "مبيعات التجزئة", "Retail Sales", parentId);

        Assert.That(center.Code, Is.EqualTo("CC-01-01"));
        Assert.That(center.NameAr, Is.EqualTo("مبيعات التجزئة"));
        Assert.That(center.NameEn, Is.EqualTo("Retail Sales"));
        Assert.That(center.ParentCostCenterId, Is.EqualTo(parentId));
    }

    [Test]
    public void SetActive_TogglesStatus()
    {
        var center = CostCenter.Create(
            Guid.NewGuid(), "CC-01", "المبيعات", null, null, true);

        center.SetActive(false);
        Assert.That(center.IsActive, Is.False);
    }
}
