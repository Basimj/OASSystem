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
using OAS.Contracts.Features.Employees.Leave;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Leave;
using OAS.Domain.Features.Employees.Settings;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Application.Features.Employees.Leave;

public sealed record GetLeaveTypesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<LeaveTypeDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveView];
}

public sealed record CreateLeaveTypeCommand(CreateLeaveTypeRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveApprove];
}

public sealed record UpdateLeaveTypeCommand(Guid Id, UpdateLeaveTypeRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveApprove];
}

public sealed record SetLeaveTypeStatusCommand(Guid Id, SetLeaveTypeStatusRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveApprove];
}

public sealed record GetEmployeeLeaveBalancesQuery(Guid EmployeeId)
    : IQuery<IReadOnlyList<EmployeeLeaveBalanceDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveView];
}

public sealed record SetEmployeeLeaveBalanceCommand(
    Guid EmployeeId,
    SetEmployeeLeaveBalanceRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveApprove];
}

public sealed record GetLeaveRequestsQuery(Guid? EmployeeId = null, byte? Status = null)
    : IQuery<IReadOnlyList<LeaveRequestDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveView];
}

public sealed record CreateLeaveRequestCommand(CreateLeaveRequestRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveRequest];
}

public sealed record UpdateLeaveRequestCommand(Guid Id, UpdateLeaveRequestRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveRequest];
}

public sealed record SubmitLeaveRequestCommand(Guid Id, LeaveTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveRequest];
}

public sealed record ApproveLeaveRequestCommand(Guid Id, LeaveTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveApprove];
}

public sealed record RejectLeaveRequestCommand(Guid Id, LeaveTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveApprove];
}

public sealed record CancelLeaveRequestCommand(Guid Id, LeaveTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LeaveRequest];
}

public sealed class LeaveTypeValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public LeaveTypeValidator()
    {
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.AnnualEntitlementDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.MaximumCarryForwardDays).GreaterThanOrEqualTo(0);
    }
}

public sealed class LeaveRequestValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public LeaveRequestValidator()
    {
        RuleFor(x => x.Request.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Request.EndDate).GreaterThanOrEqualTo(x => x.Request.StartDate);
    }
}

public sealed class GetLeaveTypesQueryHandler(IReadRepository<LeaveType, Guid> repository)
    : IRequestHandler<GetLeaveTypesQuery, IReadOnlyList<LeaveTypeDto>>
{
    public async Task<IReadOnlyList<LeaveTypeDto>> Handle(
        GetLeaveTypesQuery request,
        CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken: cancellationToken))
        .Where(x => !request.ActiveOnly || x.IsActive)
        .OrderBy(x => x.NameAr)
        .Select(Map)
        .ToArray();

    internal static LeaveTypeDto Map(LeaveType leaveType) =>
        new(
            leaveType.Id,
            leaveType.LeaveTypeCode,
            leaveType.NameAr,
            leaveType.NameEn,
            leaveType.IsPaid,
            leaveType.RequiresBalance,
            (byte)leaveType.AccrualMethod,
            (byte)leaveType.DayCountingMethod,
            leaveType.AnnualEntitlementDays,
            leaveType.MaximumCarryForwardDays,
            leaveType.ProrateOnHire,
            leaveType.IsActive,
            Convert.ToBase64String(leaveType.RowVersion),
            leaveType.IsEncashableOnTermination);
}

public sealed class CreateLeaveTypeCommandHandler(
    IRepository<LeaveType, Guid> repository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateLeaveTypeCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateLeaveTypeCommand request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var number = await sequenceNumberGenerator.NextAsync(
            "LeaveTypeCodeSequence",
            cancellationToken);

        var leaveType = LeaveType.Create(
            id,
            $"LVT-{number:000000}",
            request.Request.NameAr,
            request.Request.NameEn,
            request.Request.IsPaid,
            request.Request.RequiresBalance,
            (LeaveAccrualMethod)request.Request.AccrualMethod,
            (LeaveDayCountingMethod)request.Request.DayCountingMethod,
            request.Request.AnnualEntitlementDays,
            request.Request.MaximumCarryForwardDays,
            request.Request.ProrateOnHire,
            request.Request.IsActive);
        leaveType.SetTerminationEncashment(request.Request.IsEncashableOnTermination);

        await repository.AddAsync(leaveType, cancellationToken);
        return id;
    }
}

public sealed class UpdateLeaveTypeCommandHandler(IRepository<LeaveType, Guid> repository)
    : IRequestHandler<UpdateLeaveTypeCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateLeaveTypeCommand request,
        CancellationToken cancellationToken)
    {
        var leaveType = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveType), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            leaveType.RowVersion,
            "leave type");

        leaveType.Update(
            request.Request.NameAr,
            request.Request.NameEn,
            request.Request.IsPaid,
            request.Request.RequiresBalance,
            (LeaveAccrualMethod)request.Request.AccrualMethod,
            (LeaveDayCountingMethod)request.Request.DayCountingMethod,
            request.Request.AnnualEntitlementDays,
            request.Request.MaximumCarryForwardDays,
            request.Request.ProrateOnHire,
            request.Request.IsActive);
        leaveType.SetTerminationEncashment(request.Request.IsEncashableOnTermination);

        repository.Update(leaveType);
        return leaveType.Id;
    }
}

public sealed class SetLeaveTypeStatusCommandHandler(IRepository<LeaveType, Guid> repository)
    : IRequestHandler<SetLeaveTypeStatusCommand, Guid>
{
    public async Task<Guid> Handle(
        SetLeaveTypeStatusCommand request,
        CancellationToken cancellationToken)
    {
        var leaveType = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveType), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            leaveType.RowVersion,
            "leave type");

        leaveType.SetActive(request.Request.IsActive);
        repository.Update(leaveType);
        return leaveType.Id;
    }
}

public sealed class GetEmployeeLeaveBalancesQueryHandler(
    IReadRepository<EmployeeLeaveBalance, Guid> balances,
    IReadRepository<LeaveType, Guid> leaveTypes)
    : IRequestHandler<GetEmployeeLeaveBalancesQuery, IReadOnlyList<EmployeeLeaveBalanceDto>>
{
    public async Task<IReadOnlyList<EmployeeLeaveBalanceDto>> Handle(
        GetEmployeeLeaveBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await balances.ListAsync(
            new Specification<EmployeeLeaveBalance>().Where(x => x.EmployeeId == request.EmployeeId),
            cancellationToken);

        var typeMap = (await leaveTypes.ListAsync(cancellationToken: cancellationToken))
            .ToDictionary(x => x.Id);

        return rows
            .OrderByDescending(x => x.LeaveYear)
            .ThenBy(x => typeMap.TryGetValue(x.LeaveTypeId, out var type) ? type.NameAr : string.Empty)
            .Select(x =>
            {
                typeMap.TryGetValue(x.LeaveTypeId, out var type);
                return new EmployeeLeaveBalanceDto(
                    x.Id,
                    x.EmployeeId,
                    x.LeaveTypeId,
                    type?.LeaveTypeCode ?? string.Empty,
                    type?.NameAr ?? string.Empty,
                    x.LeaveYear,
                    x.OpeningBalanceDays,
                    x.AccruedDays,
                    x.UsedDays,
                    x.AdjustmentDays,
                    x.AvailableDays,
                    Convert.ToBase64String(x.RowVersion));
            })
            .ToArray();
    }
}

public sealed class SetEmployeeLeaveBalanceCommandHandler(
    IRepository<EmployeeLeaveBalance, Guid> balances,
    IReadRepository<Employee, Guid> employees,
    IReadRepository<LeaveType, Guid> leaveTypes,
    IEmployeeHrOperationLock operationLock)
    : IRequestHandler<SetEmployeeLeaveBalanceCommand, Guid>
{
    public async Task<Guid> Handle(
        SetEmployeeLeaveBalanceCommand request,
        CancellationToken cancellationToken)
    {
        _ = await employees.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        _ = await leaveTypes.GetByIdAsync(request.Request.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveType), request.Request.LeaveTypeId);

        await operationLock.AcquireAsync(request.EmployeeId, cancellationToken);

        var found = (await balances.ListAsync(
                new Specification<EmployeeLeaveBalance>().Where(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.LeaveTypeId == request.Request.LeaveTypeId &&
                    x.LeaveYear == request.Request.LeaveYear),
                cancellationToken))
            .SingleOrDefault();

        if (found is null)
        {
            var id = Guid.NewGuid();
            var balance = EmployeeLeaveBalance.Create(
                id,
                request.EmployeeId,
                request.Request.LeaveTypeId,
                request.Request.LeaveYear,
                request.Request.OpeningBalanceDays,
                request.Request.AccruedDays,
                0,
                request.Request.AdjustmentDays);

            await balances.AddAsync(balance, cancellationToken);
            return id;
        }

        var tracked = await balances.GetForUpdateAsync(found.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeLeaveBalance), found.Id);

        if (!string.IsNullOrWhiteSpace(request.Request.RowVersion))
        {
            HrOperationsHelpers.EnsureRowVersion(
                request.Request.RowVersion,
                tracked.RowVersion,
                "leave balance");
        }

        tracked.SetValues(
            request.Request.OpeningBalanceDays,
            request.Request.AccruedDays,
            tracked.UsedDays,
            request.Request.AdjustmentDays);

        balances.Update(tracked);
        return tracked.Id;
    }
}

public sealed class GetLeaveRequestsQueryHandler(
    IReadRepository<LeaveRequest, Guid> repository,
    IReadRepository<Employee, Guid> employees)
    : IRequestHandler<GetLeaveRequestsQuery, IReadOnlyList<LeaveRequestDto>>
{
    public async Task<IReadOnlyList<LeaveRequestDto>> Handle(
        GetLeaveRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await repository.ListAsync(
            new Specification<LeaveRequest>().Where(x =>
                (!request.EmployeeId.HasValue || x.EmployeeId == request.EmployeeId.Value) &&
                (!request.Status.HasValue || (byte)x.Status == request.Status.Value)),
            cancellationToken);

        var employeeMap = (await employees.ListAsync(cancellationToken: cancellationToken))
            .ToDictionary(x => x.Id);

        return rows
            .OrderByDescending(x => x.StartDate)
            .Select(x =>
            {
                employeeMap.TryGetValue(x.EmployeeId, out var employee);
                return new LeaveRequestDto(
                    x.Id,
                    x.LeaveRequestCode,
                    x.EmployeeId,
                    employee?.EmployeeCode ?? string.Empty,
                    employee?.DisplayName ?? string.Empty,
                    x.LeaveTypeId,
                    x.LeaveTypeCodeSnapshot,
                    x.LeaveTypeNameSnapshot,
                    x.StartDate,
                    x.EndDate,
                    x.RequestedDays,
                    x.ApprovedDays,
                    (byte)x.Status,
                    x.Reason,
                    x.BalanceOverrideUsed,
                    x.BalanceOverrideReason,
                    Convert.ToBase64String(x.RowVersion));
            })
            .ToArray();
    }
}

public sealed class CreateLeaveRequestCommandHandler(
    IRepository<LeaveRequest, Guid> repository,
    IReadRepository<Employee, Guid> employees,
    IReadRepository<LeaveType, Guid> leaveTypes,
    IReadRepository<HrSettings, Guid> settings,
    LeaveDayCalculationService calculator,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateLeaveRequestCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdAsync(request.Request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.Request.EmployeeId);

        if (!employee.IsActive)
        {
            throw new ConflictException(
                "employee_inactive",
                "Inactive employees cannot create leave requests.");
        }

        var leaveType = await leaveTypes.GetByIdAsync(request.Request.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveType), request.Request.LeaveTypeId);

        if (!leaveType.IsActive)
        {
            throw new ConflictException("leave_type_inactive", "Selected leave type is inactive.");
        }

        await EnsureSameLeaveYearAsync(
            request.Request.StartDate,
            request.Request.EndDate,
            settings,
            cancellationToken);

        await EnsureNoOverlapAsync(
            repository,
            employee.Id,
            request.Request.StartDate,
            request.Request.EndDate,
            null,
            cancellationToken);

        var requestedDays = await calculator.CalculateAsync(
            employee.Id,
            leaveType.DayCountingMethod,
            request.Request.StartDate,
            request.Request.EndDate,
            cancellationToken);

        if (requestedDays <= 0)
        {
            throw new ConflictException(
                "leave_days_zero",
                "Selected leave range contains no chargeable leave days.");
        }

        var sequence = await sequenceNumberGenerator.NextAsync(
            "LeaveRequestCodeSequence",
            cancellationToken);

        var id = Guid.NewGuid();
        var leaveRequest = LeaveRequest.Create(
            id,
            $"LVR-{request.Request.StartDate.Year:0000}-{sequence:000000}",
            employee.Id,
            leaveType,
            request.Request.StartDate,
            request.Request.EndDate,
            requestedDays,
            request.Request.Reason);

        await repository.AddAsync(leaveRequest, cancellationToken);
        return id;
    }

    internal static async Task EnsureNoOverlapAsync(
        IReadRepository<LeaveRequest, Guid> repository,
        Guid employeeId,
        DateOnly start,
        DateOnly end,
        Guid? ignoredRequestId,
        CancellationToken cancellationToken)
    {
        var overlap = await repository.CountAsync(
            new Specification<LeaveRequest>().Where(x =>
                x.EmployeeId == employeeId &&
                (!ignoredRequestId.HasValue || x.Id != ignoredRequestId.Value) &&
                x.Status != LeaveRequestStatus.Rejected &&
                x.Status != LeaveRequestStatus.Cancelled &&
                x.StartDate <= end &&
                x.EndDate >= start),
            cancellationToken);

        if (overlap > 0)
        {
            throw new ConflictException(
                "leave_request_overlap",
                "The employee already has an overlapping leave request.");
        }
    }

    internal static async Task<short> EnsureSameLeaveYearAsync(
        DateOnly start,
        DateOnly end,
        IReadRepository<HrSettings, Guid> settings,
        CancellationToken cancellationToken)
    {
        var hrSettings = await settings.GetByIdAsync(HrSettings.SingletonId, cancellationToken);
        var startMonth = hrSettings?.LeaveYearStartMonth ?? 1;

        short ResolveYear(DateOnly date) =>
            (short)(date.Month >= startMonth ? date.Year : date.Year - 1);

        var leaveYear = ResolveYear(start);
        if (leaveYear != ResolveYear(end))
        {
            throw new ConflictException(
                "leave_cross_year_not_supported",
                "A leave request cannot cross two configured leave years in this version.");
        }

        return leaveYear;
    }
}

public sealed class UpdateLeaveRequestCommandHandler(
    IRepository<LeaveRequest, Guid> repository,
    IReadRepository<LeaveType, Guid> leaveTypes,
    IReadRepository<HrSettings, Guid> settings,
    LeaveDayCalculationService calculator)
    : IRequestHandler<UpdateLeaveRequestCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveRequest), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            leaveRequest.RowVersion,
            "leave request");

        var leaveType = await leaveTypes.GetByIdAsync(leaveRequest.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveType), leaveRequest.LeaveTypeId);

        await CreateLeaveRequestCommandHandler.EnsureSameLeaveYearAsync(
            request.Request.StartDate,
            request.Request.EndDate,
            settings,
            cancellationToken);

        await CreateLeaveRequestCommandHandler.EnsureNoOverlapAsync(
            repository,
            leaveRequest.EmployeeId,
            request.Request.StartDate,
            request.Request.EndDate,
            leaveRequest.Id,
            cancellationToken);

        var requestedDays = await calculator.CalculateAsync(
            leaveRequest.EmployeeId,
            leaveType.DayCountingMethod,
            request.Request.StartDate,
            request.Request.EndDate,
            cancellationToken);

        leaveRequest.UpdateDraft(
            request.Request.StartDate,
            request.Request.EndDate,
            requestedDays,
            request.Request.Reason);

        repository.Update(leaveRequest);
        return leaveRequest.Id;
    }
}

public sealed class SubmitLeaveRequestCommandHandler(
    IRepository<LeaveRequest, Guid> repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<SubmitLeaveRequestCommand, Guid>
{
    public async Task<Guid> Handle(
        SubmitLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveRequest), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            leaveRequest.RowVersion,
            "leave request");

        leaveRequest.Submit(
            HrOperationsHelpers.Actor(currentUser),
            timeProvider.GetUtcNow());

        repository.Update(leaveRequest);
        return leaveRequest.Id;
    }
}

public sealed class ApproveLeaveRequestCommandHandler(
    IRepository<LeaveRequest, Guid> leaveRequests,
    IRepository<EmployeeLeaveBalance, Guid> balances,
    IRepository<AttendanceRecord, Guid> attendance,
    IReadRepository<LeaveType, Guid> leaveTypes,
    IReadRepository<HrSettings, Guid> settings,
    IEmployeeHrOperationLock operationLock,
    IPermissionChecker permissions,
    AttendanceScheduleService scheduleService,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ApproveLeaveRequestCommand, Guid>
{
    public async Task<Guid> Handle(
        ApproveLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await leaveRequests.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveRequest), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            leaveRequest.RowVersion,
            "leave request");

        await operationLock.AcquireAsync(leaveRequest.EmployeeId, cancellationToken);

        await CreateLeaveRequestCommandHandler.EnsureNoOverlapAsync(
            leaveRequests,
            leaveRequest.EmployeeId,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.Id,
            cancellationToken);

        var approvedDays = request.Request.ApprovedDays ?? leaveRequest.RequestedDays;
        if (approvedDays != leaveRequest.RequestedDays)
        {
            throw new ConflictException(
                "leave_partial_approval_not_supported",
                "Partial approval of a date-range leave request is not supported in this version.");
        }

        var existingAttendance = await attendance.ListAsync(
            new Specification<AttendanceRecord>().Where(x =>
                x.EmployeeId == leaveRequest.EmployeeId &&
                x.AttendanceDate >= leaveRequest.StartDate &&
                x.AttendanceDate <= leaveRequest.EndDate),
            cancellationToken);

        foreach (var record in existingAttendance)
        {
            if (record.ApprovalStatus == AttendanceApprovalStatus.Approved)
            {
                throw new ConflictException(
                    "leave_attendance_approved",
                    "Approved attendance exists inside the requested leave range. Reopen the attendance record before approving leave.");
            }

            if (record.ApprovalStatus == AttendanceApprovalStatus.PendingApproval)
            {
                throw new ConflictException(
                    "leave_attendance_pending",
                    "Attendance pending approval exists inside the requested leave range. Resolve it before approving leave.");
            }

            if (record.CheckInAtUtc.HasValue || record.CheckOutAtUtc.HasValue)
            {
                throw new ConflictException(
                    "leave_attendance_conflict",
                    "Worked attendance exists inside the requested leave range.");
            }
        }

        var leaveType = await leaveTypes.GetByIdAsync(leaveRequest.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveType), leaveRequest.LeaveTypeId);

        var leaveYear = await CreateLeaveRequestCommandHandler.EnsureSameLeaveYearAsync(
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            settings,
            cancellationToken);

        var overrideUsed = false;
        if (leaveType.RequiresBalance)
        {
            var found = (await balances.ListAsync(
                    new Specification<EmployeeLeaveBalance>().Where(x =>
                        x.EmployeeId == leaveRequest.EmployeeId &&
                        x.LeaveTypeId == leaveRequest.LeaveTypeId &&
                        x.LeaveYear == leaveYear),
                    cancellationToken))
                .SingleOrDefault();

            if (found is null)
            {
                throw new ConflictException(
                    "leave_balance_required",
                    "No leave balance exists for this employee, leave type and leave year.");
            }

            var balance = await balances.GetForUpdateAsync(found.Id, cancellationToken)
                ?? throw new ConflictException("leave_balance_required", "Leave balance is unavailable.");

            overrideUsed = balance.AvailableDays < approvedDays;
            if (overrideUsed && !request.Request.OverrideBalance)
            {
                throw new ConflictException("leave_balance_insufficient", "Insufficient leave balance.");
            }

            if (overrideUsed &&
                !await permissions.HasPermissionAsync(HrPermissions.LeaveOverride, cancellationToken))
            {
                throw new ForbiddenException();
            }

            balance.Consume(approvedDays, overrideUsed);
            balances.Update(balance);
        }

        leaveRequest.Approve(
            approvedDays,
            overrideUsed,
            overrideUsed ? request.Request.OverrideReason : null,
            HrOperationsHelpers.Actor(currentUser),
            timeProvider.GetUtcNow());

        leaveRequests.Update(leaveRequest);

        var existingByDate = existingAttendance.ToDictionary(x => x.AttendanceDate);
        for (var date = leaveRequest.StartDate;
             date <= leaveRequest.EndDate;
             date = date.AddDays(1))
        {
            var schedule = await scheduleService.ResolveAsync(
                leaveRequest.EmployeeId,
                date,
                cancellationToken,
                leaveOverride: leaveRequest);

            if (existingByDate.TryGetValue(date, out var existing))
            {
                var tracked = await attendance.GetForUpdateAsync(existing.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(AttendanceRecord), existing.Id);

                AttendanceScheduleService.Apply(tracked, schedule);
                attendance.Update(tracked);
                continue;
            }

            var created = AttendanceRecord.Create(
                Guid.NewGuid(),
                leaveRequest.EmployeeId,
                date);

            AttendanceScheduleService.Apply(created, schedule);
            await attendance.AddAsync(created, cancellationToken);
        }

        return leaveRequest.Id;
    }
}

public sealed class RejectLeaveRequestCommandHandler(
    IRepository<LeaveRequest, Guid> repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<RejectLeaveRequestCommand, Guid>
{
    public async Task<Guid> Handle(
        RejectLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveRequest), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            leaveRequest.RowVersion,
            "leave request");

        leaveRequest.Reject(
            request.Request.Reason ?? string.Empty,
            HrOperationsHelpers.Actor(currentUser),
            timeProvider.GetUtcNow());

        repository.Update(leaveRequest);
        return leaveRequest.Id;
    }
}

public sealed class CancelLeaveRequestCommandHandler(
    IRepository<LeaveRequest, Guid> leaveRequests,
    IRepository<EmployeeLeaveBalance, Guid> balances,
    IRepository<AttendanceRecord, Guid> attendance,
    IReadRepository<HrSettings, Guid> settings,
    IEmployeeHrOperationLock operationLock,
    IPermissionChecker permissions,
    AttendanceScheduleService scheduleService,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CancelLeaveRequestCommand, Guid>
{
    public async Task<Guid> Handle(
        CancelLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await leaveRequests.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LeaveRequest), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            leaveRequest.RowVersion,
            "leave request");

        await operationLock.AcquireAsync(leaveRequest.EmployeeId, cancellationToken);

        var wasApproved = leaveRequest.Status == LeaveRequestStatus.Approved;
        if (wasApproved &&
            !await permissions.HasPermissionAsync(HrPermissions.LeaveApprove, cancellationToken))
        {
            throw new ForbiddenException();
        }

        var relatedAttendance = await attendance.ListAsync(
            new Specification<AttendanceRecord>().Where(x =>
                x.SourceLeaveRequestId == leaveRequest.Id),
            cancellationToken);

        if (wasApproved && relatedAttendance.Any(x => x.EmployeePayrollId.HasValue))
        {
            throw new ConflictException(
                "leave_payroll_locked",
                "Leave cannot be cancelled because related attendance was consumed by a posted payroll. Create a payroll adjustment in a later period instead.");
        }

        if (wasApproved && relatedAttendance.Any(x =>
                x.ApprovalStatus is AttendanceApprovalStatus.Approved or AttendanceApprovalStatus.PendingApproval))
        {
            throw new ConflictException(
                "leave_attendance_locked",
                "Attendance generated from this leave is approved or pending approval. Resolve attendance before cancelling leave.");
        }

        if (wasApproved && leaveRequest.RequiresBalanceSnapshot && leaveRequest.ApprovedDays is > 0)
        {
            var leaveYear = await CreateLeaveRequestCommandHandler.EnsureSameLeaveYearAsync(
                leaveRequest.StartDate,
                leaveRequest.EndDate,
                settings,
                cancellationToken);

            var found = (await balances.ListAsync(
                    new Specification<EmployeeLeaveBalance>().Where(x =>
                        x.EmployeeId == leaveRequest.EmployeeId &&
                        x.LeaveTypeId == leaveRequest.LeaveTypeId &&
                        x.LeaveYear == leaveYear),
                    cancellationToken))
                .SingleOrDefault();

            if (found is not null)
            {
                var balance = await balances.GetForUpdateAsync(found.Id, cancellationToken);
                if (balance is not null)
                {
                    balance.Restore(leaveRequest.ApprovedDays.Value);
                    balances.Update(balance);
                }
            }
        }

        leaveRequest.Cancel(
            request.Request.Reason ?? "Cancelled",
            HrOperationsHelpers.Actor(currentUser),
            timeProvider.GetUtcNow());

        leaveRequests.Update(leaveRequest);

        foreach (var existing in relatedAttendance)
        {
            var tracked = await attendance.GetForUpdateAsync(existing.Id, cancellationToken);
            if (tracked is null)
            {
                continue;
            }

            var schedule = await scheduleService.ResolveAsync(
                leaveRequest.EmployeeId,
                tracked.AttendanceDate,
                cancellationToken,
                excludedLeaveRequestId: leaveRequest.Id);

            AttendanceScheduleService.Apply(tracked, schedule);
            attendance.Update(tracked);
        }

        return leaveRequest.Id;
    }
}
