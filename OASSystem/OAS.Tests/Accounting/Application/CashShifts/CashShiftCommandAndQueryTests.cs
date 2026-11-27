using NUnit.Framework;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.CashShifts.Commands.SetCashShiftStatus;
using OAS.Infrastructure.Accounting.Persistence.Queries;
using OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;
using OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;
using OAS.Application.Accounting.CashShifts.Mapping;
using OAS.Application.Common.Exceptions;
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
    private FakeRepository<CashAccount, Guid> _cashAccounts = null!;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeRepository<CashShift, Guid>();
        _currentUser = new FakeCurrentUser();
        _sequenceGenerator = new FakeSequenceNumberGenerator();
        _mapper = new CashShiftMapper();
        _cashAccountId = Guid.NewGuid();
        _cashAccounts = new FakeRepository<CashAccount, Guid>(new[]
        {
            CashAccount.Create(_cashAccountId, "CASH-1", "Cashbox", Guid.NewGuid(), Guid.NewGuid(), true, true)
        });
    }

    [Test]
    public async Task CreateCashShiftCommandHandler_OpensNewShift()
    {
        var handler = new CreateCashShiftCommandHandler(
            _repository,
            _sequenceGenerator,
            _currentUser,
            TimeProvider.System, _cashAccounts);

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
    public async Task CreateCashShiftCommandHandler_PreventsSecondOpenShiftForSameCashbox()
    {
        var alreadyOpen = CashShift.Create(
            Guid.NewGuid(), "CS-2026-000001", _cashAccountId,
            Guid.Parse(_currentUser.UserId!), DateTime.UtcNow, 100m,
            DomainCashShiftStatus.Open);
        await _repository.AddAsync(alreadyOpen);
        var handler = new CreateCashShiftCommandHandler(
            _repository, _sequenceGenerator, _currentUser, TimeProvider.System, _cashAccounts);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(new CreateCashShiftCommand(
                new CreateCashShiftRequest(_cashAccountId, 100m)), CancellationToken.None));
    }

    [Test]
    public async Task CloseCashShiftCommandHandler_ClosesShiftAndCalculatesDifference()
    {
        var shift = CashShift.Create(
            Guid.NewGuid(), "CS-2026-000001", _cashAccountId,
            Guid.Parse(_currentUser.UserId!), DateTime.UtcNow, 300m,
            DomainCashShiftStatus.Open);
        shift.StartClosing(expectedClosingBalance: 300m); // stale value must be refreshed
        await _repository.AddAsync(shift);

        var handler = new CloseCashShiftCommandHandler(
            _repository,
            _currentUser,
            TimeProvider.System, new FixedBalanceService(800m));

        var request = new SetCashShiftClosingRequest(
            ActualClosingBalance: 780m,
            RowVersion: Convert.ToBase64String(shift.RowVersion));

        var command = new CloseCashShiftCommand(shift.Id, request);
        await handler.Handle(command, CancellationToken.None);

        Assert.That(shift.Status, Is.EqualTo(DomainCashShiftStatus.Closed));
        Assert.That(shift.ActualClosingBalance, Is.EqualTo(780m));
        Assert.That(shift.ExpectedClosingBalance, Is.EqualTo(800m));
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

    [Test]
    public async Task CloseShift_RequiresCountButAcceptsExplicitZero()
    {
        var shift = CashShift.Create(Guid.NewGuid(), "CS-2026-000002", _cashAccountId,
            Guid.Parse(_currentUser.UserId!), DateTime.UtcNow, 100m, DomainCashShiftStatus.Open);
        await _repository.AddAsync(shift);
        var handler = new CloseCashShiftCommandHandler(_repository, _currentUser, TimeProvider.System, new FixedBalanceService(250m));
        var version = Convert.ToBase64String(shift.RowVersion);
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(
            new CloseCashShiftCommand(shift.Id, new SetCashShiftClosingRequest(null, version)), CancellationToken.None));
        Assert.That(shift.Status, Is.EqualTo(DomainCashShiftStatus.Open));
        await handler.Handle(new CloseCashShiftCommand(shift.Id, new SetCashShiftClosingRequest(0m, version)), CancellationToken.None);
        Assert.That(shift.ExpectedClosingBalance, Is.EqualTo(250m));
        Assert.That(shift.DifferenceAmount, Is.EqualTo(-250m));
    }

    [Test]
    public async Task GenericStatusCommand_CannotCloseWithoutActualCount()
    {
        var shift = CashShift.Create(Guid.NewGuid(), "CS-2026-000003", _cashAccountId,
            Guid.Parse(_currentUser.UserId!), DateTime.UtcNow, 100m, DomainCashShiftStatus.Open);
        await _repository.AddAsync(shift);
        var handler = new SetCashShiftStatusCommandHandler(_repository, new FakePermissionChecker(), TimeProvider.System, new FixedBalanceService(200m));
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(
            new SetCashShiftStatusCommand(shift.Id, new SetCashShiftStatusRequest(CashShiftStatus.Closed, Convert.ToBase64String(shift.RowVersion))), CancellationToken.None));
        Assert.That(shift.Status, Is.EqualTo(DomainCashShiftStatus.Open));
        Assert.That(shift.ActualClosingBalance, Is.Null);
    }

    [Test]
    public void OriginalCurrencyMovement_UsesHistoricalAmountAndDirection()
    {
        var usd = Guid.NewGuid();
        var yer = Guid.NewGuid();
        Assert.That(CashShiftBalanceService.ResolveMovement(usd, yer, usd, 50m, 0m, 25000m, 0m, "JV-1"), Is.EqualTo(50m));
        Assert.That(CashShiftBalanceService.ResolveMovement(usd, yer, usd, 0m, 20m, 0m, 10000m, "JV-2"), Is.EqualTo(-20m));
        Assert.That(CashShiftBalanceService.ResolveMovement(yer, yer, null, null, null, 100m, 0m, "JV-3"), Is.EqualTo(100m));
        Assert.Throws<ConflictException>(() => CashShiftBalanceService.ResolveMovement(usd, yer, null, null, null, 25000m, 0m, "JV-4"));
    }

    [Test]
    public void Validator_DistinguishesMissingCountFromZero()
    {
        var validator = new CloseCashShiftCommandValidator();
        Assert.That(validator.Validate(new CloseCashShiftCommand(Guid.NewGuid(), new SetCashShiftClosingRequest(null, "AA=="))).IsValid, Is.False);
        Assert.That(validator.Validate(new CloseCashShiftCommand(Guid.NewGuid(), new SetCashShiftClosingRequest(0m, "AA=="))).IsValid, Is.True);
    }

    private sealed class FixedBalanceService(decimal expected) : ICashShiftBalanceService
    {
        public Task<decimal> CalculateExpectedAsync(CashShift shift, DateTime throughUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(expected);
    }
}
