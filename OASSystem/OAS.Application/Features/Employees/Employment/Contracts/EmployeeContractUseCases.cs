using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.Contracts;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Application.Features.Employees.Employment.Contracts;

public sealed record GetEmployeeContractsQuery(Guid EmployeeId) : IQuery<IReadOnlyList<EmployeeContractDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.ContractsView];
}
public sealed record GetEmployeeContractByIdQuery(Guid ContractId) : IQuery<EmployeeContractDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.ContractsView];
}
public sealed record CreateEmployeeContractCommand(Guid EmployeeId, CreateEmployeeContractRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.ContractsCreate];
}
public sealed record UpdateEmployeeContractCommand(Guid ContractId, UpdateEmployeeContractRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.ContractsEdit];
}
public sealed record ActivateEmployeeContractCommand(Guid ContractId, ContractLifecycleRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.ContractsActivate];
}
public sealed record TerminateEmployeeContractCommand(Guid ContractId, TerminateEmployeeContractRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.ContractsTerminate];
}
public sealed record CancelEmployeeContractCommand(Guid ContractId, ContractLifecycleRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.ContractsEdit];
}

public sealed class CreateEmployeeContractValidator : AbstractValidator<CreateEmployeeContractCommand>
{
    public CreateEmployeeContractValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.ContractType).Must(v => Enum.IsDefined(typeof(EmploymentContractType), v)).WithErrorCode("contract_type_invalid");
        RuleFor(x => x.Request.CurrencyId).NotEmpty();
        RuleFor(x => x.Request.WorkingHoursPerDay).Must(v => v is null || (v > 0 && v <= 24));
        RuleFor(x => x.Request.WorkingDaysPerWeek).Must(v => v is null || (v > 0 && v <= 7));
        RuleFor(x => x.Request.Notes).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));
    }
}
public sealed class UpdateEmployeeContractValidator : AbstractValidator<UpdateEmployeeContractCommand>
{
    public UpdateEmployeeContractValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Request.ContractType).Must(v => Enum.IsDefined(typeof(EmploymentContractType), v)).WithErrorCode("contract_type_invalid");
        RuleFor(x => x.Request.CurrencyId).NotEmpty();
        RuleFor(x => x.Request.WorkingHoursPerDay).Must(v => v is null || (v > 0 && v <= 24));
        RuleFor(x => x.Request.WorkingDaysPerWeek).Must(v => v is null || (v > 0 && v <= 7));
        RuleFor(x => x.Request.Notes).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }
    private static bool BeBase64(string value) { try { Convert.FromBase64String(value); return true; } catch { return false; } }
}

public sealed class ActivateEmployeeContractValidator : AbstractValidator<ActivateEmployeeContractCommand>
{
    public ActivateEmployeeContractValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }

    private static bool BeBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}

public sealed class TerminateEmployeeContractValidator : AbstractValidator<TerminateEmployeeContractCommand>
{
    public TerminateEmployeeContractValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.TerminationEffectiveDate).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }

    private static bool BeBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}

public sealed class CancelEmployeeContractValidator : AbstractValidator<CancelEmployeeContractCommand>
{
    public CancelEmployeeContractValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }

    private static bool BeBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}

internal static class EmployeeContractMapping
{
    public static EmployeeContractDto ToDto(EmployeeContract x, string currencyCode) => new(x.Id, x.ContractCode, x.EmployeeId, (byte)x.ContractType, (byte)x.Status, x.StartDate, x.EndDate, x.ProbationEndDate, x.WorkingHoursPerDay, x.WorkingDaysPerWeek, x.CurrencyId, currencyCode, x.Notes, x.ActivatedBy, x.ActivatedAtUtc, x.TerminatedBy, x.TerminatedAtUtc, x.TerminationEffectiveDate, x.TerminationReason, Convert.ToBase64String(x.RowVersion));
}

public sealed class GetEmployeeContractsQueryHandler(IReadRepository<EmployeeContract, Guid> contracts, IHRAccountingReferencePort accounting) : IRequestHandler<GetEmployeeContractsQuery, IReadOnlyList<EmployeeContractDto>>
{
    public async Task<IReadOnlyList<EmployeeContractDto>> Handle(GetEmployeeContractsQuery request, CancellationToken ct)
    {
        var items = await contracts.ListAsync(new Specification<EmployeeContract>().Where(x => x.EmployeeId == request.EmployeeId), ct);
        var result = new List<EmployeeContractDto>(items.Count);
        foreach (var item in items.OrderByDescending(x => x.StartDate))
        {
            var currency = await accounting.GetCurrencyAsync(item.CurrencyId, ct);
            result.Add(EmployeeContractMapping.ToDto(item, currency?.Code ?? string.Empty));
        }
        return result;
    }
}
public sealed class GetEmployeeContractByIdQueryHandler(IReadRepository<EmployeeContract, Guid> contracts, IHRAccountingReferencePort accounting) : IRequestHandler<GetEmployeeContractByIdQuery, EmployeeContractDto>
{
    public async Task<EmployeeContractDto> Handle(GetEmployeeContractByIdQuery request, CancellationToken ct)
    {
        var item = await contracts.GetByIdAsync(request.ContractId, ct) ?? throw new NotFoundException(nameof(EmployeeContract), request.ContractId);
        var currency = await accounting.GetCurrencyAsync(item.CurrencyId, ct);
        return EmployeeContractMapping.ToDto(item, currency?.Code ?? string.Empty);
    }
}
public sealed class CreateEmployeeContractCommandHandler(IRepository<EmployeeContract, Guid> contracts, IReadRepository<Employee, Guid> employees, IHRAccountingReferencePort accounting, ISequenceNumberGenerator sequences) : IRequestHandler<CreateEmployeeContractCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeContractCommand request, CancellationToken ct)
    {
        var employee = await employees.GetByIdAsync(request.EmployeeId, ct) ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);
        if (!employee.IsActive) throw new ConflictException("employee_inactive", "Contracts cannot be created for an inactive employee.");
        await ValidateCurrency(request.Request.CurrencyId, accounting, ct);
        var number = await sequences.NextAsync("EmployeeContractCodeSequence", ct);
        var code = $"CTR-{request.Request.StartDate.Year:0000}-{number:000000}";
        var item = EmployeeContract.Create(Guid.NewGuid(), code, request.EmployeeId, (EmploymentContractType)request.Request.ContractType, request.Request.StartDate, request.Request.EndDate, request.Request.ProbationEndDate, request.Request.WorkingHoursPerDay, request.Request.WorkingDaysPerWeek, request.Request.CurrencyId, request.Request.Notes);
        await contracts.AddAsync(item, ct);
        return item.Id;
    }
    internal static async Task ValidateCurrency(Guid currencyId, IHRAccountingReferencePort accounting, CancellationToken ct)
    {
        var currency = await accounting.GetCurrencyAsync(currencyId, ct) ?? throw new ConflictException("currency_not_found", "The selected currency does not exist.");
        if (!currency.IsActive) throw new ConflictException("currency_inactive", "The selected currency is inactive.");
    }
}
public sealed class UpdateEmployeeContractCommandHandler(IRepository<EmployeeContract, Guid> contracts, IHRAccountingReferencePort accounting) : IRequestHandler<UpdateEmployeeContractCommand, Guid>
{
    public async Task<Guid> Handle(UpdateEmployeeContractCommand request, CancellationToken ct)
    {
        var item = await contracts.GetForUpdateAsync(request.ContractId, ct) ?? throw new NotFoundException(nameof(EmployeeContract), request.ContractId);
        EnsureRowVersion(request.Request.RowVersion, item.RowVersion);
        await CreateEmployeeContractCommandHandler.ValidateCurrency(request.Request.CurrencyId, accounting, ct);
        item.UpdateDraft((EmploymentContractType)request.Request.ContractType, request.Request.StartDate, request.Request.EndDate, request.Request.ProbationEndDate, request.Request.WorkingHoursPerDay, request.Request.WorkingDaysPerWeek, request.Request.CurrencyId, request.Request.Notes);
        contracts.Update(item);
        return item.Id;
    }
    internal static void EnsureRowVersion(string incoming, byte[] current)
    {
        if (!Convert.FromBase64String(incoming).SequenceEqual(current)) throw new ConcurrencyException("The contract was changed by another operation. Reload it and try again.");
    }
}
public sealed class ActivateEmployeeContractCommandHandler(
    IRepository<EmployeeContract, Guid> contracts,
    IReadRepository<Employee, Guid> employees,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<ActivateEmployeeContractCommand, Guid>
{
    public async Task<Guid> Handle(ActivateEmployeeContractCommand request, CancellationToken ct)
    {
        var item = await contracts.GetForUpdateAsync(request.ContractId, ct) ?? throw new NotFoundException(nameof(EmployeeContract), request.ContractId);
        UpdateEmployeeContractCommandHandler.EnsureRowVersion(request.Request.RowVersion, item.RowVersion);
        var employee = await employees.GetByIdAsync(item.EmployeeId, ct)
            ?? throw new NotFoundException(nameof(Employee), item.EmployeeId);
        if (!employee.IsActive)
            throw new ConflictException("employee_inactive", "An inactive employee cannot have a contract activated.");
        if (await contracts.CountAsync(new Specification<EmployeeContract>().Where(x => x.EmployeeId == item.EmployeeId && x.Status == EmploymentContractStatus.Active && x.Id != item.Id), ct) > 0)
            throw new ConflictException("contract_active_exists", "The employee already has an active contract.");
        item.Activate(currentUser.UserId, timeProvider.GetUtcNow());
        contracts.Update(item);
        return item.Id;
    }
}
public sealed class TerminateEmployeeContractCommandHandler(IRepository<EmployeeContract, Guid> contracts, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<TerminateEmployeeContractCommand, Guid>
{
    public async Task<Guid> Handle(TerminateEmployeeContractCommand request, CancellationToken ct)
    {
        var item = await contracts.GetForUpdateAsync(request.ContractId, ct) ?? throw new NotFoundException(nameof(EmployeeContract), request.ContractId);
        UpdateEmployeeContractCommandHandler.EnsureRowVersion(request.Request.RowVersion, item.RowVersion);
        item.Terminate(request.Request.Reason, currentUser.UserId, timeProvider.GetUtcNow(), request.Request.TerminationEffectiveDate);
        contracts.Update(item);
        return item.Id;
    }
}
public sealed class CancelEmployeeContractCommandHandler(IRepository<EmployeeContract, Guid> contracts) : IRequestHandler<CancelEmployeeContractCommand, Guid>
{
    public async Task<Guid> Handle(CancelEmployeeContractCommand request, CancellationToken ct)
    {
        var item = await contracts.GetForUpdateAsync(request.ContractId, ct) ?? throw new NotFoundException(nameof(EmployeeContract), request.ContractId);
        UpdateEmployeeContractCommandHandler.EnsureRowVersion(request.Request.RowVersion, item.RowVersion);
        item.Cancel();
        contracts.Update(item);
        return item.Id;
    }
}
