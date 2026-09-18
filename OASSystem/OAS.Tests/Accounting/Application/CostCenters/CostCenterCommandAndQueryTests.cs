using NUnit.Framework;
using OAS.Application.Accounting.CostCenters.Commands.CreateCostCenter;
using OAS.Application.Accounting.CostCenters.Commands.SetCostCenterStatus;
using OAS.Application.Accounting.CostCenters.Commands.UpdateCostCenter;
using OAS.Application.Accounting.CostCenters.Mapping;
using OAS.Application.Accounting.CostCenters.Queries.GetCostCenterById;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.CostCenters;

[TestFixture]
public class CostCenterCommandAndQueryTests
{
    private FakeRepository<CostCenter, Guid> _repository = null!;
    private CostCenterMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeRepository<CostCenter, Guid>();
        _mapper = new CostCenterMapper();
    }

    [Test]
    public async Task CreateCostCenterCommandHandler_CreatesNewCostCenter()
    {
        var handler = new CreateCostCenterCommandHandler(_repository, _mapper);
        var request = new CreateCostCenterRequest(
            Code: "CC-01",
            NameAr: "مركز المبيعات",
            NameEn: "Sales Center",
            ParentCostCenterId: null,
            IsActive: true);

        var command = new CreateCostCenterCommand(request);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo("CC-01"));
        Assert.That(_repository.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateCostCenterCommandHandler_UpdatesExistingCenter()
    {
        var existing = CostCenter.Create(Guid.NewGuid(), "CC-01", "المبيعات", null, null, true);
        await _repository.AddAsync(existing);

        var handler = new UpdateCostCenterCommandHandler(_repository, _mapper);
        var request = new UpdateCostCenterRequest(
            Code: "CC-01-M",
            NameAr: "إدارة المبيعات",
            NameEn: "Sales Management",
            ParentCostCenterId: null,
            IsActive: true,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var command = new UpdateCostCenterCommand(existing.Id, request);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result.Code, Is.EqualTo("CC-01-M"));
        Assert.That(result.NameAr, Is.EqualTo("إدارة المبيعات"));
    }
}
