using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Overtime;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Overtime;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Application.Features.Employees.Overtime;
public sealed record GetOvertimeQuery(Guid? EmployeeId=null, byte? Status=null):IQuery<IReadOnlyList<OvertimeRecordDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.OvertimeView];
}
public sealed record CreateOvertimeCommand(CreateOvertimeRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.OvertimeCreate];
}
public sealed record UpdateOvertimeCommand(Guid Id, UpdateOvertimeRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.OvertimeCreate];
}
public sealed record SubmitOvertimeCommand(Guid Id, OvertimeTransitionRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.OvertimeCreate];
}
public sealed record ApproveOvertimeCommand(Guid Id, OvertimeTransitionRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.OvertimeApprove];
}
public sealed record RejectOvertimeCommand(Guid Id, OvertimeTransitionRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.OvertimeApprove];
}
public sealed record CancelOvertimeCommand(Guid Id, OvertimeTransitionRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.OvertimeCreate];
}

public sealed class CreateOvertimeValidator : AbstractValidator<CreateOvertimeCommand>
{
    public CreateOvertimeValidator()
    {
        RuleFor(x=>x.Request.EmployeeId).NotEmpty();
        RuleFor(x=>x.Request.RequestedMinutes).GreaterThan(0);
        RuleFor(x=>x.Request.RateMultiplier).GreaterThan(0);
    }
}

public sealed class GetOvertimeQueryHandler(IReadRepository<OvertimeRecord, Guid> repo, IReadRepository<Employee, Guid> employees):IRequestHandler<GetOvertimeQuery, IReadOnlyList<OvertimeRecordDto>>
{
    public async Task<IReadOnlyList<OvertimeRecordDto>> Handle(GetOvertimeQuery r, CancellationToken ct)
    {
        var rows=await repo.ListAsync(new Specification<OvertimeRecord>().Where(x=>(!r.EmployeeId.HasValue||x.EmployeeId==r.EmployeeId.Value)&&(!r.Status.HasValue||(byte)x.Status==r.Status.Value)), ct);
        var map=(await employees.ListAsync(cancellationToken:ct)).ToDictionary(x=>x.Id);
        return rows.OrderByDescending(x=>x.WorkDate).Select(x=>
        {
            map.TryGetValue(x.EmployeeId, out var e);return new OvertimeRecordDto(x.Id, x.OvertimeCode, x.EmployeeId, e?.EmployeeCode??string.Empty, e?.DisplayName??string.Empty, x.AttendanceRecordId, x.WorkDate, x.RequestedMinutes, x.ApprovedMinutes, x.RateMultiplier, (byte)x.Status, x.Reason, Convert.ToBase64String(x.RowVersion));
        }
        ).ToArray();
    }
}

public sealed class CreateOvertimeCommandHandler(IRepository<OvertimeRecord, Guid> repo, IReadRepository<Employee, Guid> employees, IReadRepository<AttendanceRecord, Guid> attendance, ISequenceNumberGenerator seq):IRequestHandler<CreateOvertimeCommand, Guid>
{
    public async Task<Guid> Handle(CreateOvertimeCommand r, CancellationToken ct)
    {
        var e=await employees.GetByIdAsync(r.Request.EmployeeId, ct)??throw new NotFoundException(nameof(Employee), r.Request.EmployeeId);
        if (!e.IsActive)throw new ConflictException("employee_inactive", "Inactive employees cannot create overtime.");
        if (r.Request.AttendanceRecordId is Guid aid)
        {
            var a=await attendance.GetByIdAsync(aid, ct)??throw new NotFoundException(nameof(AttendanceRecord), aid);
            if (a.EmployeeId!=e.Id||a.AttendanceDate!=r.Request.WorkDate)throw new ConflictException("overtime_attendance_mismatch", "Attendance does not match employee/work date.");
            if (a.ApprovalStatus!=AttendanceApprovalStatus.Approved)throw new ConflictException("overtime_attendance_not_approved", "Attendance must be approved before overtime is requested from it.");
            if (r.Request.RequestedMinutes>a.OvertimeMinutes)throw new ConflictException("overtime_exceeds_attendance", "Requested overtime exceeds calculated attendance overtime.");
            if (await repo.CountAsync(new Specification<OvertimeRecord>().Where(x=>x.AttendanceRecordId==aid&&x.Status!=OvertimeStatus.Cancelled&&x.Status!=OvertimeStatus.Rejected), ct)>0)throw new ConflictException("overtime_attendance_exists", "An overtime record already exists for this attendance.");
        }
        var n=await seq.NextAsync("OvertimeCodeSequence", ct);
        var id=Guid.NewGuid();
        await repo.AddAsync(OvertimeRecord.Create(id, $"OT-{r.Request.WorkDate.Year:0000}-{n:000000}", e.Id, r.Request.AttendanceRecordId, r.Request.WorkDate, r.Request.RequestedMinutes, r.Request.RateMultiplier, r.Request.Reason), ct);
        return id;
    }
}

public sealed class UpdateOvertimeCommandHandler(IRepository<OvertimeRecord, Guid> repo, IReadRepository<AttendanceRecord, Guid> attendance):IRequestHandler<UpdateOvertimeCommand, Guid>
{
    public async Task<Guid> Handle(UpdateOvertimeCommand r, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(r.Id, ct)??throw new NotFoundException(nameof(OvertimeRecord), r.Id);
        HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion, x.RowVersion, "overtime");
        if (x.AttendanceRecordId is Guid aid)
        {
            var a=await attendance.GetByIdAsync(aid, ct)??throw new NotFoundException(nameof(AttendanceRecord), aid);
            if (r.Request.RequestedMinutes>a.OvertimeMinutes)throw new ConflictException("overtime_exceeds_attendance", "Requested overtime exceeds calculated attendance overtime.");
        }
        x.UpdateDraft(r.Request.RequestedMinutes, r.Request.RateMultiplier, r.Request.Reason);
        repo.Update(x);
        return x.Id;
    }
}
public abstract class OvertimeTransitionBase(IRepository<OvertimeRecord, Guid> repo, ICurrentUser user, TimeProvider time)
{
    protected async Task<OvertimeRecord> Load(Guid id, string rv, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(id, ct)??throw new NotFoundException(nameof(OvertimeRecord), id);
        HrOperationsHelpers.EnsureRowVersion(rv, x.RowVersion, "overtime");
        return x;
    }
    protected string? Actor=>HrOperationsHelpers.Actor(user);
    protected DateTimeOffset Now=>time.GetUtcNow();
    protected void Save(OvertimeRecord x)=>repo.Update(x);
}

public sealed class SubmitOvertimeCommandHandler(IRepository<OvertimeRecord, Guid> repo, ICurrentUser u, TimeProvider t):OvertimeTransitionBase(repo, u, t), IRequestHandler<SubmitOvertimeCommand, Guid>
{
    public async Task<Guid> Handle(SubmitOvertimeCommand r, CancellationToken ct)
    {
        var x=await Load(r.Id, r.Request.RowVersion, ct);
        x.Submit(Actor, Now);
        Save(x);
        return x.Id;
    }
}

public sealed class ApproveOvertimeCommandHandler(IRepository<OvertimeRecord, Guid> repo, ICurrentUser u, TimeProvider t):OvertimeTransitionBase(repo, u, t), IRequestHandler<ApproveOvertimeCommand, Guid>
{
    public async Task<Guid> Handle(ApproveOvertimeCommand r, CancellationToken ct)
    {
        var x=await Load(r.Id, r.Request.RowVersion, ct);
        x.Approve(r.Request.ApprovedMinutes??x.RequestedMinutes, Actor, Now);
        Save(x);
        return x.Id;
    }
}

public sealed class RejectOvertimeCommandHandler(IRepository<OvertimeRecord, Guid> repo, ICurrentUser u, TimeProvider t):OvertimeTransitionBase(repo, u, t), IRequestHandler<RejectOvertimeCommand, Guid>
{
    public async Task<Guid> Handle(RejectOvertimeCommand r, CancellationToken ct)
    {
        var x=await Load(r.Id, r.Request.RowVersion, ct);
        x.Reject(r.Request.Reason??string.Empty, Actor, Now);
        Save(x);
        return x.Id;
    }
}

public sealed class CancelOvertimeCommandHandler(IRepository<OvertimeRecord, Guid> repo, ICurrentUser u, TimeProvider t):OvertimeTransitionBase(repo, u, t), IRequestHandler<CancelOvertimeCommand, Guid>
{
    public async Task<Guid> Handle(CancelOvertimeCommand r, CancellationToken ct)
    {
        var x=await Load(r.Id, r.Request.RowVersion, ct);
        x.Cancel(r.Request.Reason??"Cancelled", Actor, Now);
        Save(x);
        return x.Id;
    }
}
