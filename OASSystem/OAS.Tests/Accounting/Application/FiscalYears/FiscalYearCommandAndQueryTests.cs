using NUnit.Framework;
using OAS.Application.Accounting.FiscalYears.Commands.CreateFiscalYear;
using OAS.Application.Accounting.FiscalYears.Mapping;
using OAS.Application.Accounting.FiscalYears.Queries.GetFiscalYearById;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainFiscalYearStatus = OAS.Domain.Accounting.Enums.FiscalYearStatus;

namespace OAS.Tests.Accounting.Application.FiscalYears;

[TestFixture]
public class FiscalYearCommandAndQueryTests
{
    private FakeRepository<FiscalYear, Guid> _repository = null!;
    private FiscalYearMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeRepository<FiscalYear, Guid>();
        _mapper = new FiscalYearMapper();
    }

    [Test]
    public async Task CreateFiscalYearCommandHandler_AddsFiscalYear()
    {
        var handler = new CreateFiscalYearCommandHandler(_repository, _mapper);
        var request = new CreateFiscalYearRequest(
            Code: "FY2026",
            Name: "السنة المالية 2026",
            StartDate: new DateOnly(2026, 1, 1),
            EndDate: new DateOnly(2026, 12, 31));

        var command = new CreateFiscalYearCommand(request);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo("FY2026"));
        Assert.That(_repository.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetFiscalYearByIdQueryHandler_ReturnsDto()
    {
        var year = FiscalYear.Create(
            Guid.NewGuid(), "FY2026", "2026",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            DomainFiscalYearStatus.Open);
        await _repository.AddAsync(year);

        var handler = new GetFiscalYearByIdQueryHandler(_repository, _mapper);
        var query = new GetFiscalYearByIdQuery(year.Id);
        var dto = await handler.Handle(query, CancellationToken.None);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Code, Is.EqualTo("FY2026"));
    }
}
