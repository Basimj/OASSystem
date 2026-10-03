using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Time;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Application.Features.Employees.Time.Shifts;
public sealed record GetWorkShiftsQuery(bool ActiveOnly=false):IQuery<IReadOnlyList<WorkShiftDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.ShiftsView];
}
public sealed record CreateWorkShiftCommand(CreateWorkShiftRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.ShiftsManage];
}
public sealed record UpdateWorkShiftCommand(Guid Id, UpdateWorkShiftRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.ShiftsManage];
}
public sealed record SetWorkShiftStatusCommand(Guid Id, SetWorkShiftStatusRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.ShiftsManage];
}
public sealed record GetEmployeeShiftAssignmentsQuery(Guid EmployeeId):IQuery<IReadOnlyList<EmployeeShiftAssignmentDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.ShiftsView];
}
public sealed record CreateEmployeeShiftAssignmentCommand(Guid EmployeeId, CreateEmployeeShiftAssignmentRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.ShiftsManage];
}
public sealed record UpdateEmployeeShiftAssignmentCommand(Guid Id, UpdateEmployeeShiftAssignmentRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.ShiftsManage];
}
public sealed record SetEmployeeShiftAssignmentStatusCommand(Guid Id, SetEmployeeShiftAssignmentStatusRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.ShiftsManage];
}

public sealed class WorkShiftRequestValidator : AbstractValidator<CreateWorkShiftCommand>
{
    public WorkShiftRequestValidator()
    {
        RuleFor(x=>x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x=>x.Request.WorkingDaysMask).InclusiveBetween((byte)1, (byte)127);
        RuleFor(x=>x.Request.BreakMinutes).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateWorkShiftValidator : AbstractValidator<UpdateWorkShiftCommand>
{
    public UpdateWorkShiftValidator()
    {
        RuleFor(x=>x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x=>x.Request.RowVersion).NotEmpty();
    }
}

public sealed class GetWorkShiftsQueryHandler(IReadRepository<WorkShift, Guid> repo):IRequestHandler<GetWorkShiftsQuery, IReadOnlyList<WorkShiftDto>>
{
    public async Task<IReadOnlyList<WorkShiftDto>> Handle(GetWorkShiftsQuery r, CancellationToken ct)=>(await repo.ListAsync(cancellationToken:ct)).Where(x=>!r.ActiveOnly||x.IsActive).OrderBy(x=>x.NameAr).Select(Map).ToArray();
    internal static WorkShiftDto Map(WorkShift x)=>new(x.Id, x.ShiftCode, x.NameAr, x.NameEn, x.StartTime, x.EndTime, x.BreakMinutes, x.GraceLateMinutes, x.GraceEarlyLeaveMinutes, x.WorkingDaysMask, x.IsFlexible, x.IsActive, x.Notes, x.ScheduledMinutes, x.CrossesMidnight, Convert.ToBase64String(x.RowVersion));
}

public sealed class CreateWorkShiftCommandHandler(IRepository<WorkShift, Guid> repo, ISequenceNumberGenerator seq):IRequestHandler<CreateWorkShiftCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkShiftCommand r, CancellationToken ct)
    {
        var id=Guid.NewGuid();
        var n=await seq.NextAsync("WorkShiftCodeSequence", ct);
        await repo.AddAsync(WorkShift.Create(id, $"SHF-{n:000000}", r.Request.NameAr, r.Request.NameEn, r.Request.StartTime, r.Request.EndTime, r.Request.BreakMinutes, r.Request.GraceLateMinutes, r.Request.GraceEarlyLeaveMinutes, r.Request.WorkingDaysMask, r.Request.IsFlexible, r.Request.IsActive, r.Request.Notes), ct);
        return id;
    }
}

public sealed class UpdateWorkShiftCommandHandler(IRepository<WorkShift, Guid> repo, IReadRepository<AttendanceRecord, Guid> attendance):IRequestHandler<UpdateWorkShiftCommand, Guid>
{
    public async Task<Guid> Handle(UpdateWorkShiftCommand r, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(r.Id, ct)??throw new NotFoundException(nameof(WorkShift), r.Id);
        HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion, x.RowVersion, "work shift");
        if (await attendance.CountAsync(new Specification<AttendanceRecord>().Where(a=>a.WorkShiftId==x.Id&&a.ApprovalStatus==OAS.Domain.Features.Employees.Enums.AttendanceApprovalStatus.Approved), ct)>0&&(x.StartTime!=r.Request.StartTime||x.EndTime!=r.Request.EndTime||x.BreakMinutes!=r.Request.BreakMinutes||x.GraceLateMinutes!=r.Request.GraceLateMinutes||x.GraceEarlyLeaveMinutes!=r.Request.GraceEarlyLeaveMinutes||x.WorkingDaysMask!=r.Request.WorkingDaysMask))throw new ConflictException("shift_in_use", "A shift used by approved attendance cannot have its time policy changed. Create a new shift version instead.");
        x.Update(r.Request.NameAr, r.Request.NameEn, r.Request.StartTime, r.Request.EndTime, r.Request.BreakMinutes, r.Request.GraceLateMinutes, r.Request.GraceEarlyLeaveMinutes, r.Request.WorkingDaysMask, r.Request.IsFlexible, r.Request.IsActive, r.Request.Notes);
        repo.Update(x);
        return x.Id;
    }
}

public sealed class SetWorkShiftStatusCommandHandler(IRepository<WorkShift, Guid> repo):IRequestHandler<SetWorkShiftStatusCommand, Guid>
{
    public async Task<Guid> Handle(SetWorkShiftStatusCommand r, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(r.Id, ct)??throw new NotFoundException(nameof(WorkShift), r.Id);
        HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion, x.RowVersion, "work shift");
        x.SetActive(r.Request.IsActive);
        repo.Update(x);
        return x.Id;
    }
}

public sealed class GetEmployeeShiftAssignmentsQueryHandler(IReadRepository<EmployeeShiftAssignment, Guid> assignments, IReadRepository<WorkShift, Guid> shifts):IRequestHandler<GetEmployeeShiftAssignmentsQuery, IReadOnlyList<EmployeeShiftAssignmentDto>>
{
    public async Task<IReadOnlyList<EmployeeShiftAssignmentDto>> Handle(GetEmployeeShiftAssignmentsQuery r, CancellationToken ct)
    {
        var a=await assignments.ListAsync(new Specification<EmployeeShiftAssignment>().Where(x=>x.EmployeeId==r.EmployeeId), ct);
        var map=(await shifts.ListAsync(cancellationToken:ct)).ToDictionary(x=>x.Id);
        return a.OrderByDescending(x=>x.EffectiveFrom).Select(x=>
        {
            map.TryGetValue(x.WorkShiftId, out var s);return new EmployeeShiftAssignmentDto(x.Id, x.EmployeeId, x.WorkShiftId, s?.ShiftCode??string.Empty, s?.NameAr??string.Empty, x.EffectiveFrom, x.EffectiveTo, x.IsActive, Convert.ToBase64String(x.RowVersion));
        }
        ).ToArray();
    }
}

public sealed class CreateEmployeeShiftAssignmentCommandHandler(IRepository<EmployeeShiftAssignment, Guid> repo, IReadRepository<Employee, Guid> employees, IReadRepository<WorkShift, Guid> shifts, IEmployeeHrOperationLock gate):IRequestHandler<CreateEmployeeShiftAssignmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeShiftAssignmentCommand r, CancellationToken ct)
    {
        var emp=await employees.GetByIdAsync(r.EmployeeId, ct)??throw new NotFoundException(nameof(Employee), r.EmployeeId);
        if (!emp.IsActive)throw new ConflictException("employee_inactive", "Inactive employees cannot receive new shift assignments.");
        var shift=await shifts.GetByIdAsync(r.Request.WorkShiftId, ct)??throw new NotFoundException(nameof(WorkShift), r.Request.WorkShiftId);
        if (!shift.IsActive)throw new ConflictException("shift_inactive", "The selected shift is inactive.");
        await gate.AcquireAsync(r.EmployeeId, ct);
        await EnsureNoOverlap(repo, r.EmployeeId, r.Request.EffectiveFrom, r.Request.EffectiveTo, null, ct);
        var id=Guid.NewGuid();
        await repo.AddAsync(EmployeeShiftAssignment.Create(id, r.EmployeeId, r.Request.WorkShiftId, r.Request.EffectiveFrom, r.Request.EffectiveTo, r.Request.IsActive), ct);
        return id;
    }
    internal static async Task EnsureNoOverlap(IReadRepository<EmployeeShiftAssignment, Guid> repo, Guid emp, DateOnly from, DateOnly? to, Guid? ignore, CancellationToken ct)
    {
        var items=await repo.ListAsync(new Specification<EmployeeShiftAssignment>().Where(x=>x.EmployeeId==emp&&x.IsActive&&(!ignore.HasValue||x.Id!=ignore.Value)), ct);
        var end=to??DateOnly.MaxValue;
        if (items.Any(x=>x.EffectiveFrom<=end&&from<=(x.EffectiveTo??DateOnly.MaxValue)))throw new ConflictException("shift_overlap", "The employee already has an overlapping active shift assignment.");
    }
}

public sealed class UpdateEmployeeShiftAssignmentCommandHandler(IRepository<EmployeeShiftAssignment, Guid> repo, IReadRepository<WorkShift, Guid> shifts, IEmployeeHrOperationLock gate):IRequestHandler<UpdateEmployeeShiftAssignmentCommand, Guid>
{
    public async Task<Guid> Handle(UpdateEmployeeShiftAssignmentCommand r, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(r.Id, ct)??throw new NotFoundException(nameof(EmployeeShiftAssignment), r.Id);
        HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion, x.RowVersion, "shift assignment");
        var shift=await shifts.GetByIdAsync(r.Request.WorkShiftId, ct)??throw new NotFoundException(nameof(WorkShift), r.Request.WorkShiftId);
        if (!shift.IsActive)throw new ConflictException("shift_inactive", "The selected shift is inactive.");
        await gate.AcquireAsync(x.EmployeeId, ct);
        await CreateEmployeeShiftAssignmentCommandHandler.EnsureNoOverlap(repo, x.EmployeeId, r.Request.EffectiveFrom, r.Request.EffectiveTo, x.Id, ct);
        x.Update(r.Request.WorkShiftId, r.Request.EffectiveFrom, r.Request.EffectiveTo, r.Request.IsActive);
        repo.Update(x);
        return x.Id;
    }
}

public sealed class SetEmployeeShiftAssignmentStatusCommandHandler(IRepository<EmployeeShiftAssignment, Guid> repo, IEmployeeHrOperationLock gate):IRequestHandler<SetEmployeeShiftAssignmentStatusCommand, Guid>
{
    public async Task<Guid> Handle(SetEmployeeShiftAssignmentStatusCommand r, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(r.Id, ct)??throw new NotFoundException(nameof(EmployeeShiftAssignment), r.Id);
        HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion, x.RowVersion, "shift assignment");
        await gate.AcquireAsync(x.EmployeeId, ct);
        if (r.Request.IsActive)await CreateEmployeeShiftAssignmentCommandHandler.EnsureNoOverlap(repo, x.EmployeeId, x.EffectiveFrom, x.EffectiveTo, x.Id, ct);
        x.SetActive(r.Request.IsActive);
        repo.Update(x);
        return x.Id;
    }
}
