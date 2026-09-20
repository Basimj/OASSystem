using NUnit.Framework;
using OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;
using OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;
using OAS.Application.Accounting.CashShifts.Mapping;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShiftById;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShifts;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainCashShiftStatus = OAS.Domain.Accounting.Enums.CashShiftStatus;

namespace OAS.Tests.Accounting.Application.CashShifts;

[TestFixture]
public class CashShiftCommandAndQueryTests
{
    private FakeRepository<CashShift, Guid> _repository = null!;
    private FakeCurrentUser _currentUser = null!;
    private FakeSequenceNumberGenerator _sequenceGenerator = null!;
    private CashShiftMapper _mapper = null!;
    private Guid _cashAccountId;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeRepository<CashShift, Guid>();
        _currentUser = new FakeCurrentUser();
        _sequenceGenerator = new FakeSequenceNumberGenerator();
        _mapper = new CashShiftMapper();
        _cashAccountId = Guid.NewGuid();
    }

    [Test]
    public async Task CreateCashShiftCommandHandler_OpensNewShift()
    {
        var handler = new CreateCashShiftCommandHandler(
            _repository,
            _sequenceGenerator,
            _currentUser,
            TimeProvider.System);

        var request = new CreateCashShiftRequest(
            CashAccountId: _cashAccountId,
            OpeningBalance: 500m);

        var command = new CreateCashShiftCommand(request);
        var shiftId = await handler.Handle(command, CancellationToken.None);

        Assert.That(shiftId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(_repository.Items.Count, Is.EqualTo(1));
        var shift = _repository.Items[0];
        Assert.That(shift.Status, Is.EqualTo(DomainCashShiftStatus.Open));
        Assert.That(shift.OpeningBalance, Is.EqualTo(500m));
    }

    [Test]
    public async Task CloseCashShiftCommandHandler_ClosesShiftAndCalculatesDifference()
    {
        var shift = CashShift.Create(
            Guid.NewGuid(), "CS-2026-000001", _cashAccountId,
            Guid.Parse(_currentUser.UserId!), DateTime.UtcNow, 300m,
            DomainCashShiftStatus.Open);
        shift.StartClosing(expectedClosingBalance: 800m);
        await _repository.AddAsync(shift);

        var handler = new CloseCashShiftCommandHandler(
            _repository,
            _currentUser,
            TimeProvider.System);

        var request = new SetCashShiftClosingRequest(
            ActualClosingBalance: 780m,
            RowVersion: Convert.ToBase64String(shift.RowVersion));

        var command = new CloseCashShiftCommand(shift.Id, request);
        await handler.Handle(command, CancellationToken.None);

        Assert.That(shift.Status, Is.EqualTo(DomainCashShiftStatus.Closed));
        Assert.That(shift.ActualClosingBalance, Is.EqualTo(780m));
        Assert.That(shift.DifferenceAmount, Is.EqualTo(-20m));
    }

    [Test]
    public async Task GetCashShiftByIdQueryHandler_ReturnsMappedDto()
    {
        var shift = CashShift.Create(
            Guid.NewGuid(), "CS-2026-000001", _cashAccountId,
            Guid.Parse(_currentUser.UserId!), DateTime.UtcNow, 400m,
            DomainCashShiftStatus.Open);
        await _repository.AddAsync(shift);

        var handler = new GetCashShiftByIdQueryHandler(_repository, _mapper);
        var query = new GetCashShiftByIdQuery(shift.Id);
        var dto = await handler.Handle(query, CancellationToken.None);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.ShiftNumber, Is.EqualTo("CS-2026-000001"));
        Assert.That(dto.OpeningBalance, Is.EqualTo(400m));
    }
}
