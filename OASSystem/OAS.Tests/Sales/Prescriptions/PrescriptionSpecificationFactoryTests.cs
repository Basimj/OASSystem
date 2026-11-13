using NUnit.Framework;
using OAS.Application.Sales.Prescriptions.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Sales.Entities;

namespace OAS.Tests.Sales.Prescriptions;

[TestFixture]
public sealed class PrescriptionSpecificationFactoryTests
{
    [Test]
    public void CustomerSearch_RespectsMatchedCustomerIds()
    {
        var customerId = Guid.NewGuid();
        var prescription = Prescription.Create(Guid.NewGuid(), "RX-2026-001", customerId, new DateOnly(2026, 11, 12));
        var factory = new PrescriptionSpecificationFactory();
        var spec = factory.CreatePageSpecification(new PageRequest { Search = "عميل تجريبي", Status = "Draft" }, [customerId]);
        Assert.That(spec.Criteria, Is.Not.Null);
        Assert.That(spec.Criteria!.Compile()(prescription), Is.True);
    }

    [Test]
    public void StatusFilter_RemainsActiveTogetherWithCustomerSearch()
    {
        var customerId = Guid.NewGuid();
        var prescription = Prescription.Create(Guid.NewGuid(), "RX-2026-002", customerId, new DateOnly(2026, 11, 12));
        var factory = new PrescriptionSpecificationFactory();
        var spec = factory.CreatePageSpecification(new PageRequest { Search = "عميل تجريبي", Status = "Cancelled" }, [customerId]);
        Assert.That(spec.Criteria, Is.Not.Null);
        Assert.That(spec.Criteria!.Compile()(prescription), Is.False);
    }

    [Test]
    public void SearchByCode_StillWorksWithoutCustomerMatch()
    {
        var prescription = Prescription.Create(Guid.NewGuid(), "RX-2026-003", Guid.NewGuid(), new DateOnly(2026, 11, 12));
        var factory = new PrescriptionSpecificationFactory();
        var spec = factory.CreatePageSpecification(new PageRequest { Search = "RX-2026" }, []);
        Assert.That(spec.Criteria!.Compile()(prescription), Is.True);
    }
}
