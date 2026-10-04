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
using OAS.Application.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Adjustments;
using OAS.Domain.Features.Employees.Adjustments;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Application.Features.Employees.Adjustments;
public sealed record GetEmployeeAdjustmentsQuery(Guid? EmployeeId=null, byte? Status=null):IQuery<IReadOnlyList<EmployeeAdjustmentDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.AdjustmentsView];
}
public sealed record CreateEmployeeAdjustmentCommand(CreateEmployeeAdjustmentRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.AdjustmentsCreate];
}
public sealed record UpdateEmployeeAdjustmentCommand(Guid Id, UpdateEmployeeAdjustmentRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.AdjustmentsCreate];
}
public sealed record SubmitEmployeeAdjustmentCommand(Guid Id, EmployeeAdjustmentTransitionRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.AdjustmentsCreate];
}
public sealed record ApproveEmployeeAdjustmentCommand(Guid Id, EmployeeAdjustmentTransitionRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.AdjustmentsApprove];
}
public sealed record RejectEmployeeAdjustmentCommand(Guid Id, EmployeeAdjustmentTransitionRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.AdjustmentsApprove];
}
public sealed record CancelEmployeeAdjustmentCommand(Guid Id, EmployeeAdjustmentTransitionRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.AdjustmentsCreate];
}

public sealed class CreateEmployeeAdjustmentValidator : AbstractValidator<CreateEmployeeAdjustmentCommand>
{
    public CreateEmployeeAdjustmentValidator()
    {
        RuleFor(x=>x.Request.EmployeeId).NotEmpty();
        RuleFor(x=>x.Request.SalaryComponentId).NotEmpty();
        RuleFor(x=>x.Request.Amount).GreaterThan(0);
        RuleFor(x=>x.Request.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class GetEmployeeAdjustmentsQueryHandler(IReadRepository<EmployeeAdjustment, Guid> repo, IReadRepository<Employee, Guid> employees):IRequestHandler<GetEmployeeAdjustmentsQuery, IReadOnlyList<EmployeeAdjustmentDto>>
{
    public async Task<IReadOnlyList<EmployeeAdjustmentDto>> Handle(GetEmployeeAdjustmentsQuery r, CancellationToken ct)
    {
        var rows=await repo.ListAsync(new Specification<EmployeeAdjustment>().Where(x=>(!r.EmployeeId.HasValue||x.EmployeeId==r.EmployeeId.Value)&&(!r.Status.HasValue||(byte)x.Status==r.Status.Value)), ct);
        var map=(await employees.ListAsync(cancellationToken:ct)).ToDictionary(x=>x.Id);
        return rows.OrderByDescending(x=>x.EffectiveDate).Select(x=>
        {
            map.TryGetValue(x.EmployeeId, out var e);return new EmployeeAdjustmentDto(x.Id, x.AdjustmentCode, x.EmployeeId, e?.EmployeeCode??string.Empty, e?.DisplayName??string.Empty, x.SalaryComponentId, x.ComponentCodeSnapshot, x.ComponentNameSnapshot, (byte)x.ComponentTypeSnapshot, (byte)x.AdjustmentType, x.EffectiveDate, x.CurrencyId, x.CurrencyCodeSnapshot, x.CurrencySymbolSnapshot, x.CurrencyDecimalPlacesSnapshot, x.Amount, (byte)x.Status, x.Reason, Convert.ToBase64String(x.RowVersion));
        }
        ).ToArray();
    }
}

public sealed class CreateEmployeeAdjustmentCommandHandler(IRepository<EmployeeAdjustment, Guid> repo, IReadRepository<Employee, Guid> employees, IReadRepository<SalaryComponent, Guid> components, IEmployeeSalaryStructureRepository salary, IHRAccountingReferencePort accounting, ISequenceNumberGenerator seq):IRequestHandler<CreateEmployeeAdjustmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeAdjustmentCommand r, CancellationToken ct)
    {
        var e=await employees.GetByIdAsync(r.Request.EmployeeId, ct)??throw new NotFoundException(nameof(Employee), r.Request.EmployeeId);
        if (!e.IsActive)throw new ConflictException("employee_inactive", "Inactive employees cannot receive new adjustments.");
        var comp=await components.GetByIdAsync(r.Request.SalaryComponentId, ct)??throw new NotFoundException(nameof(SalaryComponent), r.Request.SalaryComponentId);
        if (!comp.IsActive)throw new ConflictException("salary_component_inactive", "Selected salary component is inactive.");
        if (comp.ComponentType==SalaryComponentType.EmployerContribution)throw new ConflictException("adjustment_component_unsupported", "Employer contribution components cannot be used as employee adjustments.");
        var structure=await salary.GetActiveForEmployeeAsync(e.Id, false, ct)??throw new ConflictException("salary_structure_active_required", "An active salary structure is required before creating employee adjustments.");
        var curr=await accounting.GetCurrencyAsync(structure.CurrencyId, ct)??throw new ConflictException("adjustment_currency_missing", "Salary currency does not exist.");
        if (!curr.IsActive)throw new ConflictException("adjustment_currency_inactive", "Salary currency is inactive.");
        var n=await seq.NextAsync("EmployeeAdjustmentCodeSequence", ct);
        var id=Guid.NewGuid();
        await repo.AddAsync(EmployeeAdjustment.Create(id, $"ADJ-{r.Request.EffectiveDate.Year:0000}-{n:000000}", e.Id, comp, (EmployeeAdjustmentType)r.Request.AdjustmentType, r.Request.EffectiveDate, curr.Id, curr.Code, curr.Symbol, curr.DecimalPlaces, r.Request.Amount, r.Request.Reason), ct);
        return id;
    }
}

public sealed class UpdateEmployeeAdjustmentCommandHandler(IRepository<EmployeeAdjustment, Guid> repo):IRequestHandler<UpdateEmployeeAdjustmentCommand, Guid>
{
    public async Task<Guid> Handle(UpdateEmployeeAdjustmentCommand r, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(r.Id, ct)??throw new NotFoundException(nameof(EmployeeAdjustment), r.Id);
        HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion, x.RowVersion, "employee adjustment");
        x.UpdateDraft((EmployeeAdjustmentType)r.Request.AdjustmentType, r.Request.Amount, r.Request.Reason);
        repo.Update(x);
        return x.Id;
    }
}
public abstract class EmployeeAdjustmentTransitionBase(IRepository<EmployeeAdjustment, Guid> repo, ICurrentUser user, TimeProvider time)
{
    protected async Task<EmployeeAdjustment> Load(Guid id, string rv, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(id, ct)??throw new NotFoundException(nameof(EmployeeAdjustment), id);
        HrOperationsHelpers.EnsureRowVersion(rv, x.RowVersion, "employee adjustment");
        return x;
    }
    protected string? Actor=>HrOperationsHelpers.Actor(user);
    protected DateTimeOffset Now=>time.GetUtcNow();
    protected void Save(EmployeeAdjustment x)=>repo.Update(x);
}

public sealed class SubmitEmployeeAdjustmentCommandHandler(IRepository<EmployeeAdjustment, Guid> r, ICurrentUser u, TimeProvider t):EmployeeAdjustmentTransitionBase(r, u, t), IRequestHandler<SubmitEmployeeAdjustmentCommand, Guid>
{
    public async Task<Guid> Handle(SubmitEmployeeAdjustmentCommand c, CancellationToken ct)
    {
        var x=await Load(c.Id, c.Request.RowVersion, ct);
        x.Submit(Actor, Now);
        Save(x);
        return x.Id;
    }
}

public sealed class ApproveEmployeeAdjustmentCommandHandler(IRepository<EmployeeAdjustment, Guid> r, ICurrentUser u, TimeProvider t):EmployeeAdjustmentTransitionBase(r, u, t), IRequestHandler<ApproveEmployeeAdjustmentCommand, Guid>
{
    public async Task<Guid> Handle(ApproveEmployeeAdjustmentCommand c, CancellationToken ct)
    {
        var x=await Load(c.Id, c.Request.RowVersion, ct);
        x.Approve(Actor, Now);
        Save(x);
        return x.Id;
    }
}

public sealed class RejectEmployeeAdjustmentCommandHandler(IRepository<EmployeeAdjustment, Guid> r, ICurrentUser u, TimeProvider t):EmployeeAdjustmentTransitionBase(r, u, t), IRequestHandler<RejectEmployeeAdjustmentCommand, Guid>
{
    public async Task<Guid> Handle(RejectEmployeeAdjustmentCommand c, CancellationToken ct)
    {
        var x=await Load(c.Id, c.Request.RowVersion, ct);
        x.Reject(c.Request.Reason??string.Empty, Actor, Now);
        Save(x);
        return x.Id;
    }
}

public sealed class CancelEmployeeAdjustmentCommandHandler(IRepository<EmployeeAdjustment, Guid> r, ICurrentUser u, TimeProvider t):EmployeeAdjustmentTransitionBase(r, u, t), IRequestHandler<CancelEmployeeAdjustmentCommand, Guid>
{
    public async Task<Guid> Handle(CancelEmployeeAdjustmentCommand c, CancellationToken ct)
    {
        var x=await Load(c.Id, c.Request.RowVersion, ct);
        x.Cancel(c.Request.Reason??"Cancelled", Actor, Now);
        Save(x);
        return x.Id;
    }
}
