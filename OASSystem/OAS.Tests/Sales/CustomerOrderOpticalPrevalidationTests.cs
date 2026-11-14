using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using OAS.API.Sales.Controllers;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;
using OAS.Domain.Sales.Entities;
using DomainEye = OAS.Domain.Sales.Enums.EyeSide;

namespace OAS.Tests.Sales;

[TestFixture]
public sealed class CustomerOrderOpticalPrevalidationTests
{
    [Test]
    public async Task Missing_catalogue_identity_is_line_error_not_exception()
    {
        var resolver = new FakeLensVariants(null);
        var response = await Controller(resolver).PrevalidateOptical(
            Request(ManualLine(1m, -0.5m, 90, null)), CancellationToken.None);
        var result = Extract(response);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo("sales_lens_variant_not_defined"));
            Assert.That(result.Issues[0].LineNumber, Is.EqualTo(1));
            Assert.That(result.Issues[0].Message, Does.Contain("SKU"));
        });
        Assert.That(resolver.LastRequest, Is.Not.Null);
    }

    [Test]
    public async Task Known_identity_passes_without_creating_an_order()
    {
        var resolver = new FakeLensVariants(new LensVariantResolution(Guid.NewGuid(), 1m, 0m, null));
        var response = await Controller(resolver).PrevalidateOptical(
            Request(ManualLine(1m, 0m, null, null)), CancellationToken.None);
        var result = Extract(response);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public async Task Manual_cylinder_requires_axis_and_missing_power_is_reported_locally()
    {
        var resolver = new FakeLensVariants(null);
        var response = await Controller(resolver).PrevalidateOptical(
            Request(ManualLine(1m, 0.75m, null, null), ManualLine(1m, null, null, null)),
            CancellationToken.None);
        var result = Extract(response);
        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Select(x => x.Code), Is.EquivalentTo(new[]
            {
                "sales_optical_manual_axis_required", "sales_optical_manual_power_required"
            }));
            Assert.That(resolver.LastRequest, Is.Null, "No catalogue query when basic inputs are invalid");
        });
    }

    [Test]
    public async Task Unprescribed_lens_without_snapshot_is_not_force_matched()
    {
        var resolver = new FakeLensVariants(null);
        var orderLine = ManualLine(0m, 0m, null, null) with { OpticalSnapshot = null };
        var result = Extract(await Controller(resolver).PrevalidateOptical(Request(orderLine), CancellationToken.None));
        Assert.That(result.IsValid, Is.True);
        Assert.That(resolver.LastRequest, Is.Null);
    }

    private static CustomerOrderOpticalPrevalidationRequest Request(params CustomerOrderLineRequest[] lines)
        => new(null, lines);

    private static CustomerOrderLineRequest ManualLine(decimal? sph, decimal? cyl, short? axis, decimal? add)
        => new(null, null, SalesLineType.Lens, Guid.NewGuid(), null, null, 1m, 0m,
            SalesDiscountType.None, null, null, null, EyeSide.RightOD, true, null,
            OpticalSnapshot: new CustomerOrderLineOpticalSnapshotRequest(
                OpticalMeasurementSource.Manual, null, EyeSide.RightOD,
                SPH: sph, CYL: cyl, Axis: axis, ADD: add));

    private static CustomerOrdersController Controller(ILensVariantResolver variants)
        => new(null!, variants, new FakePrescriptionValidator());

    private static CustomerOrderOpticalPrevalidationResult Extract(
        ActionResult<CustomerOrderOpticalPrevalidationResult> result)
        => ((OkObjectResult)result.Result!).Value as CustomerOrderOpticalPrevalidationResult
            ?? throw new InvalidOperationException("No optical validation response");

    private sealed class FakeLensVariants(LensVariantResolution? answer) : ILensVariantResolver
    {
        public LensVariantMatchRequest? LastRequest { get; private set; }
        public Task<LensVariantResolution?> TryResolveExactAsync(
            LensVariantMatchRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(answer);
        }
        public Task<LensVariantResolution> ResolveAsync(
            LensVariantMatchRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Prevalidation must not use throwing ResolveAsync");
    }

    private sealed class FakePrescriptionValidator : ISalesPrescriptionValidator
    {
        public Task<PrescriptionEyeDetail?> ValidateLineAsync(
            Guid? prescriptionRevisionId, DomainEye? eye, bool prescriptionRequired,
            CancellationToken cancellationToken = default)
            => Task.FromResult<PrescriptionEyeDetail?>(null);
    }
}
