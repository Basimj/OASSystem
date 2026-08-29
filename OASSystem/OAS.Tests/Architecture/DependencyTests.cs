using System.Reflection;
using NUnit.Framework;

namespace OAS.Tests.Architecture;

[TestFixture]
public sealed class DependencyTests
{
    private static readonly string[] ClientAllowedOasReferences = ["OAS.Contracts", "OAS.UiLib"];

    [Test]
    public void DomainMustNotReferenceFrameworkOrOuterLayers()
    {
        AssertDoesNotReference(typeof(OAS.Domain.Common.Entities.Entity<>).Assembly,
            "OAS.Application", "OAS.Infrastructure", "OAS.API", "OAS.Client", "OAS.Contracts",
            "Microsoft.EntityFrameworkCore", "MediatR", "AutoMapper", "Mapster");
    }

    [Test]
    public void ContractsMustNotReferenceBusinessOrOuterLayers()
    {
        AssertDoesNotReference(typeof(OAS.Contracts.Common.Pagination.PageRequest).Assembly,
            "OAS.Domain", "OAS.Application", "OAS.Infrastructure", "OAS.API", "OAS.Client");
    }

    [Test]
    public void ApplicationMustNotReferenceOuterLayers()
    {
        AssertDoesNotReference(typeof(OAS.Application.DependencyInjection).Assembly,
            "OAS.Infrastructure", "OAS.API", "OAS.Client", "OAS.UiLib");
    }

    [Test]
    public void InfrastructureMustNotReferencePresentationLayers()
    {
        AssertDoesNotReference(typeof(OAS.Infrastructure.DependencyInjection).Assembly,
            "OAS.API", "OAS.Client", "OAS.UiLib");
    }

    [Test]
    public void ClientMustOnlySeeContractsAndUiLibraryFromOasLayers()
    {
        var assembly = typeof(OAS.Client.ClientAssemblyMarker).Assembly;
        var oasReferences = assembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x?.StartsWith("OAS.", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.That(oasReferences, Is.SubsetOf(ClientAllowedOasReferences));
        Assert.That(oasReferences, Does.Not.Contain("OAS.Application"));
        Assert.That(oasReferences, Does.Not.Contain("OAS.Infrastructure"));
        Assert.That(oasReferences, Does.Not.Contain("OAS.Domain"));
    }

    [Test]
    public void UiLibMustNotReferenceApplicationLayers()
    {
        AssertDoesNotReference(typeof(OAS.UiLib.Localization.UiLibSharedResources).Assembly,
            "OAS.Domain", "OAS.Contracts", "OAS.Application", "OAS.Infrastructure", "OAS.API", "OAS.Client");
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbidden)
    {
        var references = assembly.GetReferencedAssemblies().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in forbidden)
            Assert.That(references, Does.Not.Contain(name), $"{assembly.GetName().Name} must not reference {name}");
    }
}
