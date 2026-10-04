using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Time;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Settings;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Application.Features.Employees.Time.Attendance;

public sealed record GetAttendanceQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? EmployeeId = null,
    Guid? DepartmentId = null,
    byte? ApprovalStatus = null)
    : IQuery<IReadOnlyList<AttendanceRecordDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.AttendanceView];
}

public sealed record GenerateAttendanceCommand(GenerateAttendanceRequest Request)
    : ICommand<int>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.AttendanceCreate];
}

public sealed record CreateAttendanceCommand(CreateAttendanceRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.AttendanceCreate];
}

public sealed record UpdateAttendanceCommand(Guid Id, UpdateAttendanceRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.AttendanceEdit];
}

public sealed record SubmitAttendanceCommand(Guid Id, AttendanceTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.AttendanceEdit];
}

public sealed record ApproveAttendanceCommand(Guid Id, AttendanceTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.AttendanceApprove];
}

public sealed record RejectAttendanceCommand(Guid Id, AttendanceTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.AttendanceApprove];
}

public sealed class GenerateAttendanceValidator : AbstractValidator<GenerateAttendanceCommand>
{
    public GenerateAttendanceValidator()
    {
        RuleFor(x => x.Request.ToDate)
            .GreaterThanOrEqualTo(x => x.Request.FromDate);

        RuleFor(x => x.Request)
            .Must(x => x.ToDate.DayNumber - x.FromDate.DayNumber <= 92)
            .WithMessage("Attendance generation range cannot exceed 93 days.");
    }
}

public sealed class CreateAttendanceValidator : AbstractValidator<CreateAttendanceCommand>
{
    public CreateAttendanceValidator()
    {
        RuleFor(x => x.Request.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.Source).InclusiveBetween((byte)1, (byte)4);
    }
}

public sealed class UpdateAttendanceValidator : AbstractValidator<UpdateAttendanceCommand>
{
    public UpdateAttendanceValidator()
    {
        RuleFor(x => x.Request.RowVersion).NotEmpty();
        RuleFor(x => x.Request.Source).InclusiveBetween((byte)1, (byte)4);
    }
}

public sealed class GetAttendanceQueryHandler(
    IReadRepository<AttendanceRecord, Guid> repository,
    IReadRepository<Employee, Guid> employees)
    : IRequestHandler<GetAttendanceQuery, IReadOnlyList<AttendanceRecordDto>>
{
    public async Task<IReadOnlyList<AttendanceRecordDto>> Handle(
        GetAttendanceQuery request,
        CancellationToken cancellationToken)
    {
        var items = await repository.ListAsync(
            new Specification<AttendanceRecord>().Where(x =>
                (!request.From.HasValue || x.AttendanceDate >= request.From.Value) &&
                (!request.To.HasValue || x.AttendanceDate <= request.To.Value) &&
                (!request.EmployeeId.HasValue || x.EmployeeId == request.EmployeeId.Value) &&
                (!request.ApprovalStatus.HasValue || (byte)x.ApprovalStatus == request.ApprovalStatus.Value)),
            cancellationToken);

        var employeeMap = (await employees.ListAsync(cancellationToken: cancellationToken))
            .Where(x => !request.DepartmentId.HasValue || x.DepartmentId == request.DepartmentId.Value)
            .ToDictionary(x => x.Id);

        return items
            .Where(x => employeeMap.ContainsKey(x.EmployeeId))
            .OrderByDescending(x => x.AttendanceDate)
            .ThenBy(x => employeeMap[x.EmployeeId].DisplayName)
            .Select(x => Map(x, employeeMap[x.EmployeeId]))
            .ToArray();
    }

    internal static AttendanceRecordDto Map(AttendanceRecord record, Employee employee) =>
        new(
            record.Id,
            record.EmployeeId,
            employee.EmployeeCode,
            employee.DisplayName,
            record.AttendanceDate,
            record.WorkShiftId,
            record.ShiftCodeSnapshot,
            record.ShiftNameSnapshot,
            record.CheckInAtUtc,
            record.CheckOutAtUtc,
            (byte)record.Source,
            (byte)record.AttendanceStatus,
            (byte)record.ApprovalStatus,
            record.ScheduledMinutes,
            record.WorkedMinutes,
            record.LateMinutes,
            record.EarlyLeaveMinutes,
            record.OvertimeMinutes,
            record.Notes,
            Convert.ToBase64String(record.RowVersion));
}

public sealed class GenerateAttendanceCommandHandler(
    IRepository<AttendanceRecord, Guid> repository,
    IReadRepository<Employee, Guid> employees,
    AttendanceScheduleService scheduleService)
    : IRequestHandler<GenerateAttendanceCommand, int>
{
    public async Task<int> Handle(
        GenerateAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var employeeList = (await employees.ListAsync(cancellationToken: cancellationToken))
            .Where(x =>
                x.IsActive &&
                (!request.Request.EmployeeId.HasValue || x.Id == request.Request.EmployeeId.Value) &&
                (!request.Request.DepartmentId.HasValue || x.DepartmentId == request.Request.DepartmentId.Value))
            .ToArray();

        var existing = (await repository.ListAsync(
                new Specification<AttendanceRecord>().Where(x =>
                    x.AttendanceDate >= request.Request.FromDate &&
                    x.AttendanceDate <= request.Request.ToDate),
                cancellationToken))
            .ToDictionary(x => (x.EmployeeId, x.AttendanceDate));

        var changed = 0;
        foreach (var employee in employeeList)
        {
            for (var date = request.Request.FromDate;
                 date <= request.Request.ToDate;
                 date = date.AddDays(1))
            {
                var schedule = await scheduleService.ResolveAsync(employee.Id, date, cancellationToken);

                if (existing.TryGetValue((employee.Id, date), out var old))
                {
                    if (old.ApprovalStatus == AttendanceApprovalStatus.Approved)
                    {
                        continue;
                    }

                    if (schedule.Status == AttendanceDayStatus.Leave &&
                        (old.CheckInAtUtc.HasValue || old.CheckOutAtUtc.HasValue))
                    {
                        throw new ConflictException(
                            "leave_attendance_conflict",
                            $"Attendance already contains worked time for employee {employee.EmployeeCode} on {date:yyyy-MM-dd}.");
                    }

                    var tracked = await repository.GetForUpdateAsync(old.Id, cancellationToken);
                    if (tracked is null)
                    {
                        continue;
                    }

                    AttendanceScheduleService.Apply(tracked, schedule);
                    repository.Update(tracked);
                    changed++;
                    continue;
                }

                var attendance = AttendanceRecord.Create(Guid.NewGuid(), employee.Id, date);
                AttendanceScheduleService.Apply(attendance, schedule);
                await repository.AddAsync(attendance, cancellationToken);
                changed++;
            }
        }

        return changed;
    }
}

public sealed class CreateAttendanceCommandHandler(
    IRepository<AttendanceRecord, Guid> repository,
    IReadRepository<Employee, Guid> employees,
    AttendanceScheduleService scheduleService)
    : IRequestHandler<CreateAttendanceCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdAsync(request.Request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.Request.EmployeeId);

        if (!employee.IsActive)
        {
            throw new ConflictException(
                "employee_inactive",
                "Inactive employees cannot receive attendance records.");
        }

        var existingCount = await repository.CountAsync(
            new Specification<AttendanceRecord>().Where(x =>
                x.EmployeeId == employee.Id &&
                x.AttendanceDate == request.Request.AttendanceDate),
            cancellationToken);

        if (existingCount > 0)
        {
            throw new ConflictException(
                "attendance_exists",
                "Attendance already exists for this employee and date.");
        }

        var schedule = await scheduleService.ResolveAsync(
            employee.Id,
            request.Request.AttendanceDate,
            cancellationToken);

        if (schedule.Status == AttendanceDayStatus.Leave &&
            (request.Request.CheckInAtUtc.HasValue || request.Request.CheckOutAtUtc.HasValue))
        {
            throw new ConflictException(
                "leave_attendance_conflict",
                "The employee has approved leave on this date.");
        }

        var attendance = AttendanceRecord.Create(
            Guid.NewGuid(),
            employee.Id,
            request.Request.AttendanceDate);

        AttendanceScheduleService.Apply(attendance, schedule);

        var variance = AttendanceScheduleService.CalculateVariance(
            schedule,
            request.Request.CheckInAtUtc,
            request.Request.CheckOutAtUtc);

        attendance.RecordTimes(
            request.Request.CheckInAtUtc,
            request.Request.CheckOutAtUtc,
            (AttendanceSource)request.Request.Source,
            variance.Late,
            variance.Early,
            request.Request.Notes);

        await repository.AddAsync(attendance, cancellationToken);
        return attendance.Id;
    }
}

public sealed class UpdateAttendanceCommandHandler(
    IRepository<AttendanceRecord, Guid> repository,
    ICurrentUser currentUser,
    IPermissionChecker permissionChecker,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateAttendanceCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var attendance = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AttendanceRecord), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            attendance.RowVersion,
            "attendance");

        var actor = HrOperationsHelpers.Actor(currentUser);
        var now = timeProvider.GetUtcNow();

        if (attendance.ApprovalStatus == AttendanceApprovalStatus.Approved)
        {
            if (!await permissionChecker.HasPermissionAsync(
                    HrPermissions.AttendanceApprove,
                    cancellationToken))
            {
                throw new ForbiddenException();
            }

            if (string.IsNullOrWhiteSpace(request.Request.CorrectionReason))
            {
                throw new ConflictException(
                    "attendance_correction_reason_required",
                    "A correction reason is required to edit approved attendance.");
            }

            attendance.ReopenForCorrection(
                request.Request.CorrectionReason,
                actor,
                now);
        }

        var variance = CalculateVariance(
            attendance,
            request.Request.CheckInAtUtc,
            request.Request.CheckOutAtUtc);

        attendance.RecordTimes(
            request.Request.CheckInAtUtc,
            request.Request.CheckOutAtUtc,
            (AttendanceSource)request.Request.Source,
            variance.Late,
            variance.Early,
            request.Request.Notes,
            request.Request.CorrectionReason,
            actor,
            now);

        repository.Update(attendance);
        return attendance.Id;
    }

    private static (int Late, int Early) CalculateVariance(
        AttendanceRecord attendance,
        DateTimeOffset? checkIn,
        DateTimeOffset? checkOut)
    {
        if (attendance.IsFlexibleSnapshot)
        {
            return (0, 0);
        }

        var late = checkIn.HasValue && attendance.ScheduledStartAtUtc.HasValue
            ? (int)Math.Max(
                0,
                (checkIn.Value - attendance.ScheduledStartAtUtc.Value).TotalMinutes -
                attendance.GraceLateMinutesSnapshot)
            : 0;

        var early = checkOut.HasValue && attendance.ScheduledEndAtUtc.HasValue
            ? (int)Math.Max(
                0,
                (attendance.ScheduledEndAtUtc.Value - checkOut.Value).TotalMinutes -
                attendance.GraceEarlyLeaveMinutesSnapshot)
            : 0;

        return (late, early);
    }
}

public abstract class AttendanceTransitionHandlerBase(
    IRepository<AttendanceRecord, Guid> repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    protected async Task<AttendanceRecord> LoadAsync(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        var attendance = await repository.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AttendanceRecord), id);

        HrOperationsHelpers.EnsureRowVersion(rowVersion, attendance.RowVersion, "attendance");
        return attendance;
    }

    protected string? Actor => HrOperationsHelpers.Actor(currentUser);
    protected DateTimeOffset Now => timeProvider.GetUtcNow();
    protected void Save(AttendanceRecord attendance) => repository.Update(attendance);
}

public sealed class SubmitAttendanceCommandHandler(
    IRepository<AttendanceRecord, Guid> repository,
    IReadRepository<HrSettings, Guid> settings,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : AttendanceTransitionHandlerBase(repository, currentUser, timeProvider),
      IRequestHandler<SubmitAttendanceCommand, Guid>
{
    public async Task<Guid> Handle(
        SubmitAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var attendance = await LoadAsync(
            request.Id,
            request.Request.RowVersion,
            cancellationToken);

        attendance.Submit(Actor, Now);

        var hrSettings = await settings.GetByIdAsync(HrSettings.SingletonId, cancellationToken);
        if (hrSettings is { RequireAttendanceApproval: false })
        {
            attendance.Approve(Actor, Now);
        }

        Save(attendance);
        return attendance.Id;
    }
}

public sealed class ApproveAttendanceCommandHandler(
    IRepository<AttendanceRecord, Guid> repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : AttendanceTransitionHandlerBase(repository, currentUser, timeProvider),
      IRequestHandler<ApproveAttendanceCommand, Guid>
{
    public async Task<Guid> Handle(
        ApproveAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var attendance = await LoadAsync(
            request.Id,
            request.Request.RowVersion,
            cancellationToken);

        attendance.Approve(Actor, Now);
        Save(attendance);
        return attendance.Id;
    }
}

public sealed class RejectAttendanceCommandHandler(
    IRepository<AttendanceRecord, Guid> repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : AttendanceTransitionHandlerBase(repository, currentUser, timeProvider),
      IRequestHandler<RejectAttendanceCommand, Guid>
{
    public async Task<Guid> Handle(
        RejectAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var attendance = await LoadAsync(
            request.Id,
            request.Request.RowVersion,
            cancellationToken);

        attendance.Reject(request.Request.Reason ?? string.Empty, Actor, Now);
        Save(attendance);
        return attendance.Id;
    }
}
