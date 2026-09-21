using NUnit.Framework;
using OAS.Application.Accounting.CostCenters.Commands.CreateCostCenter;
using OAS.Application.Accounting.CostCenters.Commands.UpdateCostCenter;
using OAS.Application.Accounting.CostCenters.Mapping;
using OAS.Application.Accounting.CostCenters.Queries.GetCostCenterById;
using OAS.Application.Common.Exceptions;
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

        var result = await handler.Handle(
            new CreateCostCenterCommand(request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Code, Is.EqualTo("CC-01"));
            Assert.That(_repository.Items.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateCostCenterCommandHandler_UpdatesExistingCenterWithoutChangingCode()
    {
        var existing = CostCenter.Create(
            Guid.NewGuid(), "CC-01", "المبيعات", null, null, true);
        await _repository.AddAsync(existing);

        var handler = new UpdateCostCenterCommandHandler(_repository, _mapper);
        var request = new UpdateCostCenterRequest(
            Code: "CC-01",
            NameAr: "إدارة المبيعات",
            NameEn: "Sales Management",
            ParentCostCenterId: null,
            IsActive: true,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var result = await handler.Handle(
            new UpdateCostCenterCommand(existing.Id, request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("CC-01"));
            Assert.That(result.NameAr, Is.EqualTo("إدارة المبيعات"));
            Assert.That(_repository.Items.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void UpdateCostCenterCommandHandler_ChangingCode_ThrowsConflictException()
    {
        var existing = CostCenter.Create(
            Guid.NewGuid(), "CC-01", "المبيعات", null, null, true);
        _repository.AddAsync(existing).GetAwaiter().GetResult();

        var handler = new UpdateCostCenterCommandHandler(_repository, _mapper);
        var request = new UpdateCostCenterRequest(
            Code: "CC-02",
            NameAr: "المبيعات",
            NameEn: null,
            ParentCostCenterId: null,
            IsActive: true,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new UpdateCostCenterCommand(existing.Id, request),
                CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("accounting_cost_center_code_immutable"));
        Assert.That(existing.Code, Is.EqualTo("CC-01"));
    }

    [Test]
    public async Task GetCostCenterByIdQueryHandler_ReturnsMappedDto()
    {
        var existing = CostCenter.Create(
            Guid.NewGuid(), "CC-01", "المبيعات", "Sales", null, true);
        await _repository.AddAsync(existing);

        var handler = new GetCostCenterByIdQueryHandler(_repository, _mapper);
        var dto = await handler.Handle(
            new GetCostCenterByIdQuery(existing.Id),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(existing.Id));
            Assert.That(dto.Code, Is.EqualTo("CC-01"));
            Assert.That(dto.NameAr, Is.EqualTo("المبيعات"));
        });
    }
}
