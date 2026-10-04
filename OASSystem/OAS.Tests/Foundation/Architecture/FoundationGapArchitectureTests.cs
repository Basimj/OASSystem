using NUnit.Framework;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Common.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Tests.Foundation.Architecture;

[TestFixture]
public sealed class FoundationGapArchitectureTests
{
    [Test]
    public void NewAuditedEntities_UseAuditableEntityBase()
    {
        var types = new[]
        {
            typeof(LensVariantDetail),
            typeof(CustomerOrderLineOpticalSnapshot),
            typeof(CustomerAdvance),
            typeof(CustomerAdvanceApplication),
            typeof(OpticalJob),
            typeof(OpticalJobLine)
        };

        foreach (var type in types)
            Assert.That(typeof(AuditableEntity<Guid>).IsAssignableFrom(type), Is.True, type.Name);
    }

    [Test]
    public void ContractEnums_MatchDomainEnumsNumerically()
    {
        AssertEnumParity<OAS.Domain.Sales.Enums.OpticalMeasurementSource, OAS.Contracts.Sales.Enums.OpticalMeasurementSource>();
        AssertEnumParity<OAS.Domain.Sales.Enums.OpticalJobStatus, OAS.Contracts.Sales.Enums.OpticalJobStatus>();
        AssertEnumParity<OAS.Domain.Accounting.Enums.CustomerAdvanceStatus, OAS.Contracts.Accounting.Enums.CustomerAdvanceStatus>();
    }

    [Test]
    public void OpticalLabDtos_DoNotExposeFinancialFields()
    {
        var forbidden = new[] { "Price", "Discount", "Cost", "Paid", "Outstanding", "CustomerAccountId", "JournalEntryId" };
        var types = new[] { typeof(OpticalJobWorkQueueDto), typeof(OpticalJobDetailsDto), typeof(OpticalJobLineDto) };

        foreach (var type in types)
        {
            var names = type.GetProperties().Select(x => x.Name).ToArray();
            foreach (var token in forbidden)
                Assert.That(names.Any(x => x.Contains(token, StringComparison.OrdinalIgnoreCase)), Is.False, $"{type.Name} leaks {token}");
        }
    }

    private static void AssertEnumParity<TDomain, TContract>()
        where TDomain : struct, Enum
        where TContract : struct, Enum
    {
        var domain = Enum.GetNames<TDomain>().Select(name => (name, value: Convert.ToInt64(Enum.Parse<TDomain>(name)))).ToArray();
        var contract = Enum.GetNames<TContract>().Select(name => (name, value: Convert.ToInt64(Enum.Parse<TContract>(name)))).ToArray();
        Assert.That(contract, Is.EqualTo(domain));
    }
}
