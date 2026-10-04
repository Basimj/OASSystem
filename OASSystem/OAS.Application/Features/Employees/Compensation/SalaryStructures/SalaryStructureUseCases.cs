using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.Compensation;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Application.Features.Employees.Compensation.SalaryStructures;

public sealed record GetEmployeeSalaryStructuresQuery(Guid EmployeeId) : IQuery<IReadOnlyList<EmployeeSalaryStructureDto>>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryStructuresView]; }
public sealed record GetEmployeeSalaryStructureByIdQuery(Guid Id) : IQuery<EmployeeSalaryStructureDto>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryStructuresView]; }
public sealed record CreateEmployeeSalaryStructureCommand(Guid EmployeeId, CreateEmployeeSalaryStructureRequest Request) : ICommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryStructuresManage]; }
public sealed record UpdateEmployeeSalaryStructureCommand(Guid Id, UpdateEmployeeSalaryStructureRequest Request) : ICommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryStructuresManage]; }
public sealed record ActivateEmployeeSalaryStructureCommand(Guid Id, SalaryStructureLifecycleRequest Request) : ICommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryStructuresActivate]; }
public sealed record CancelEmployeeSalaryStructureCommand(Guid Id, SalaryStructureLifecycleRequest Request) : ICommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryStructuresManage]; }

public sealed class CreateEmployeeSalaryStructureValidator : AbstractValidator<CreateEmployeeSalaryStructureCommand>
{
    public CreateEmployeeSalaryStructureValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.CurrencyId).NotEmpty();
        RuleFor(x => x.Request.Lines).NotEmpty();
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.SalaryComponentId).NotEmpty();
            line.RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.Percentage).GreaterThan(0).When(x => x.Percentage.HasValue);
        });
    }
}
public sealed class UpdateEmployeeSalaryStructureValidator : AbstractValidator<UpdateEmployeeSalaryStructureCommand>
{
    public UpdateEmployeeSalaryStructureValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.CurrencyId).NotEmpty();
        RuleFor(x => x.Request.Lines).NotEmpty();
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.SalaryComponentId).NotEmpty();
            line.RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.Percentage).GreaterThan(0).When(x => x.Percentage.HasValue);
        });
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }
    private static bool BeBase64(string value) { try { Convert.FromBase64String(value); return true; } catch { return false; } }
}

public sealed class ActivateEmployeeSalaryStructureValidator : AbstractValidator<ActivateEmployeeSalaryStructureCommand>
{
    public ActivateEmployeeSalaryStructureValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }

    private static bool BeBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}

public sealed class CancelEmployeeSalaryStructureValidator : AbstractValidator<CancelEmployeeSalaryStructureCommand>
{
    public CancelEmployeeSalaryStructureValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }

    private static bool BeBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}

internal static class SalaryStructureMapping
{
    public static EmployeeSalaryStructureDto ToDto(EmployeeSalaryStructure x, string currencyCode) => new(x.Id, x.StructureCode, x.EmployeeId, x.ContractId, x.CurrencyId, currencyCode, x.EffectiveFrom, x.EffectiveTo, (byte)x.Status, x.Notes, x.ApprovedBy, x.ApprovedAtUtc, x.Lines.OrderBy(l => l.ComponentNameSnapshot).Select(l => new EmployeeSalaryStructureLineDto(l.Id, l.SalaryComponentId, l.ComponentCodeSnapshot, l.ComponentNameSnapshot, (byte)l.ComponentTypeSnapshot, (byte)l.CalculationMethodSnapshot, l.IsBasicSalarySnapshot, l.Amount, l.Percentage, l.DebitPostingRoleSnapshot, l.CreditPostingRoleSnapshot)).ToArray(), Convert.ToBase64String(x.RowVersion));
}

public sealed class GetEmployeeSalaryStructuresQueryHandler(IEmployeeSalaryStructureRepository structures, IHRAccountingReferencePort accounting) : IRequestHandler<GetEmployeeSalaryStructuresQuery, IReadOnlyList<EmployeeSalaryStructureDto>>
{
    public async Task<IReadOnlyList<EmployeeSalaryStructureDto>> Handle(GetEmployeeSalaryStructuresQuery request, CancellationToken ct)
    {
        var items = await structures.ListForEmployeeAsync(request.EmployeeId, ct);
        var result = new List<EmployeeSalaryStructureDto>();
        foreach (var item in items)
        {
            var currency = await accounting.GetCurrencyAsync(item.CurrencyId, ct);
            result.Add(SalaryStructureMapping.ToDto(item, currency?.Code ?? string.Empty));
        }
        return result;
    }
}
public sealed class GetEmployeeSalaryStructureByIdQueryHandler(IEmployeeSalaryStructureRepository structures, IHRAccountingReferencePort accounting) : IRequestHandler<GetEmployeeSalaryStructureByIdQuery, EmployeeSalaryStructureDto>
{
    public async Task<EmployeeSalaryStructureDto> Handle(GetEmployeeSalaryStructureByIdQuery request, CancellationToken ct)
    {
        var item = await structures.GetByIdAsync(request.Id, false, ct) ?? throw new NotFoundException(nameof(EmployeeSalaryStructure), request.Id);
        var currency = await accounting.GetCurrencyAsync(item.CurrencyId, ct);
        return SalaryStructureMapping.ToDto(item, currency?.Code ?? string.Empty);
    }
}
public sealed class CreateEmployeeSalaryStructureCommandHandler(IEmployeeSalaryStructureRepository structures, IReadRepository<Employee, Guid> employees, IReadRepository<EmployeeContract, Guid> contracts, IReadRepository<SalaryComponent, Guid> components, IHRAccountingReferencePort accounting, ISequenceNumberGenerator sequences) : IRequestHandler<CreateEmployeeSalaryStructureCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeSalaryStructureCommand request, CancellationToken ct)
    {
        var employee = await employees.GetByIdAsync(request.EmployeeId, ct) ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);
        if (!employee.IsActive) throw new ConflictException("employee_inactive", "Salary structures cannot be created for an inactive employee.");
        await ValidateContractAsync(request.EmployeeId, request.Request.ContractId, request.Request.CurrencyId, request.Request.EffectiveFrom, request.Request.EffectiveTo, false, contracts, ct);
        await OAS.Application.Features.Employees.Employment.Contracts.CreateEmployeeContractCommandHandler.ValidateCurrency(request.Request.CurrencyId, accounting, ct);
        var id = Guid.NewGuid();
        var number = await sequences.NextAsync("SalaryStructureCodeSequence", ct);
        var item = EmployeeSalaryStructure.Create(id, $"SST-{request.Request.EffectiveFrom.Year:0000}-{number:000000}", request.EmployeeId, request.Request.ContractId, request.Request.CurrencyId, request.Request.EffectiveFrom, request.Request.EffectiveTo, request.Request.Notes);
        item.ReplaceLines(await BuildLines(id, request.Request.Lines, components, ct));
        await structures.AddAsync(item, ct);
        return item.Id;
    }

    internal static async Task<List<EmployeeSalaryStructureLine>> BuildLines(Guid structureId, IReadOnlyList<SalaryStructureLineRequest> requests, IReadRepository<SalaryComponent, Guid> components, CancellationToken ct)
    {
        var result = new List<EmployeeSalaryStructureLine>(requests.Count);
        foreach (var line in requests)
        {
            var component = await components.GetByIdAsync(line.SalaryComponentId, ct) ?? throw new ConflictException("salary_component_not_found", "A selected salary component does not exist.");
            if (!component.IsActive) throw new ConflictException("salary_component_inactive", $"Salary component '{component.NameAr}' is inactive.");
            result.Add(EmployeeSalaryStructureLine.Create(Guid.NewGuid(), structureId, component, line.Amount, line.Percentage));
        }
        return result;
    }

    internal static async Task ValidateContractAsync(
        Guid employeeId,
        Guid? contractId,
        Guid currencyId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        bool requireActive,
        IReadRepository<EmployeeContract, Guid> contracts,
        CancellationToken ct)
    {
        if (contractId is null)
            return;

        var contract = await contracts.GetByIdAsync(contractId.Value, ct)
            ?? throw new ConflictException("contract_not_found", "The selected contract does not exist.");

        if (contract.EmployeeId != employeeId)
            throw new ConflictException("salary_structure_contract_employee_mismatch", "The selected contract belongs to another employee.");

        if (contract.CurrencyId != currencyId)
            throw new ConflictException("salary_structure_currency_mismatch", "Salary structure currency must match the selected contract currency.");

        if (requireActive && contract.Status != EmploymentContractStatus.Active)
            throw new ConflictException("salary_structure_contract_not_active", "The selected contract must be active before the salary structure can be activated.");

        if (contract.Status is EmploymentContractStatus.Cancelled or EmploymentContractStatus.Terminated or EmploymentContractStatus.Expired)
            throw new ConflictException("salary_structure_contract_invalid", "The selected contract is no longer valid.");

        if (effectiveFrom < contract.StartDate)
            throw new ConflictException("salary_structure_contract_date_mismatch", "Salary structure cannot start before the selected contract.");

        if (contract.EndDate.HasValue)
        {
            if (effectiveFrom > contract.EndDate.Value)
                throw new ConflictException("salary_structure_contract_date_mismatch", "Salary structure cannot start after the selected contract ends.");

            if (effectiveTo.HasValue && effectiveTo.Value > contract.EndDate.Value)
                throw new ConflictException("salary_structure_contract_date_mismatch", "Salary structure cannot end after the selected contract ends.");
        }
    }
}
public sealed class UpdateEmployeeSalaryStructureCommandHandler(IEmployeeSalaryStructureRepository structures, IReadRepository<EmployeeContract, Guid> contracts, IReadRepository<SalaryComponent, Guid> components, IHRAccountingReferencePort accounting) : IRequestHandler<UpdateEmployeeSalaryStructureCommand, Guid>
{
    public async Task<Guid> Handle(UpdateEmployeeSalaryStructureCommand request, CancellationToken ct)
    {
        var item = await structures.GetByIdAsync(request.Id, true, ct) ?? throw new NotFoundException(nameof(EmployeeSalaryStructure), request.Id);
        EnsureRowVersion(request.Request.RowVersion, item.RowVersion);
        await CreateEmployeeSalaryStructureCommandHandler.ValidateContractAsync(item.EmployeeId, request.Request.ContractId, request.Request.CurrencyId, request.Request.EffectiveFrom, request.Request.EffectiveTo, false, contracts, ct);
        await OAS.Application.Features.Employees.Employment.Contracts.CreateEmployeeContractCommandHandler.ValidateCurrency(request.Request.CurrencyId, accounting, ct);
        item.UpdateDraft(request.Request.ContractId, request.Request.CurrencyId, request.Request.EffectiveFrom, request.Request.EffectiveTo, request.Request.Notes);
        item.ReplaceLines(await CreateEmployeeSalaryStructureCommandHandler.BuildLines(item.Id, request.Request.Lines, components, ct));
        structures.Update(item);
        return item.Id;
    }
    internal static void EnsureRowVersion(string incoming, byte[] current)
    {
        if (!Convert.FromBase64String(incoming).SequenceEqual(current)) throw new ConcurrencyException("The salary structure was changed by another operation. Reload it and try again.");
    }
}
public sealed class ActivateEmployeeSalaryStructureCommandHandler(
    IEmployeeSalaryStructureRepository structures,
    IReadRepository<EmployeeContract, Guid> contracts,
    IReadRepository<Employee, Guid> employees,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ActivateEmployeeSalaryStructureCommand, Guid>
{
    public async Task<Guid> Handle(ActivateEmployeeSalaryStructureCommand request, CancellationToken ct)
    {
        var item = await structures.GetByIdAsync(request.Id, true, ct) ?? throw new NotFoundException(nameof(EmployeeSalaryStructure), request.Id);
        UpdateEmployeeSalaryStructureCommandHandler.EnsureRowVersion(request.Request.RowVersion, item.RowVersion);
        var employee = await employees.GetByIdAsync(item.EmployeeId, ct)
            ?? throw new NotFoundException(nameof(Employee), item.EmployeeId);
        if (!employee.IsActive)
            throw new ConflictException("employee_inactive", "An inactive employee cannot have a salary structure activated.");
        await CreateEmployeeSalaryStructureCommandHandler.ValidateContractAsync(item.EmployeeId, item.ContractId, item.CurrencyId, item.EffectiveFrom, item.EffectiveTo, true, contracts, ct);
        var active = await structures.GetActiveForEmployeeAsync(item.EmployeeId, true, ct);
        if (active is not null && active.Id != item.Id)
        {
            if (item.EffectiveFrom <= active.EffectiveFrom) throw new ConflictException("salary_structure_effective_date_invalid", "The new salary structure must start after the current active structure.");
            active.Supersede(item.EffectiveFrom.AddDays(-1));
            structures.Update(active);

            // Flush the old active row inside the current transaction before activating
            // the new one. This prevents the filtered unique index from observing two
            // active structures during an arbitrary SQL update order.
            await unitOfWork.SaveChangesAsync(ct);
        }

        item.Activate(currentUser.UserId, timeProvider.GetUtcNow());
        structures.Update(item);
        return item.Id;
    }
}
public sealed class CancelEmployeeSalaryStructureCommandHandler(IEmployeeSalaryStructureRepository structures) : IRequestHandler<CancelEmployeeSalaryStructureCommand, Guid>
{
    public async Task<Guid> Handle(CancelEmployeeSalaryStructureCommand request, CancellationToken ct)
    {
        var item = await structures.GetByIdAsync(request.Id, true, ct) ?? throw new NotFoundException(nameof(EmployeeSalaryStructure), request.Id);
        UpdateEmployeeSalaryStructureCommandHandler.EnsureRowVersion(request.Request.RowVersion, item.RowVersion);
        item.Cancel();
        structures.Update(item);
        return item.Id;
    }
}
