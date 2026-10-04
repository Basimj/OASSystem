using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Leave;
using OAS.Domain.Features.Employees.Settings;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Application.Features.Employees.Services;

public sealed record AttendanceScheduleResolution(
    WorkShift? Shift,
    AttendanceDayStatus Status,
    Guid? LeaveRequestId,
    Guid? HolidayId,
    string? TimeZoneId,
    DateTimeOffset? ScheduledStartUtc,
    DateTimeOffset? ScheduledEndUtc,
    int ScheduledMinutes);

public sealed class AttendanceScheduleService(
    IReadRepository<EmployeeShiftAssignment, Guid> assignments,
    IReadRepository<WorkShift, Guid> shifts,
    IReadRepository<Holiday, Guid> holidays,
    IReadRepository<LeaveRequest, Guid> leaves,
    IReadRepository<HrSettings, Guid> settings,
    IHRTimeZoneService zones)
{
    public async Task<AttendanceScheduleResolution> ResolveAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken,
        LeaveRequest? leaveOverride = null,
        Guid? excludedLeaveRequestId = null)
    {
        var config = await settings.GetByIdAsync(HrSettings.SingletonId, cancellationToken);
        var timeZoneId = config?.TimeZoneId;

        var matchingAssignments = await assignments.ListAsync(
            new Specification<EmployeeShiftAssignment>().Where(x =>
                x.EmployeeId == employeeId &&
                x.IsActive &&
                x.EffectiveFrom <= date &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= date)),
            cancellationToken);

        if (matchingAssignments.Count > 1)
        {
            throw new ConflictException(
                "shift_overlap",
                "More than one active shift assignment covers this date.");
        }

        var holiday = (await holidays.ListAsync(
                new Specification<Holiday>().Where(x =>
                    x.IsActive &&
                    x.StartDate <= date &&
                    x.EndDate >= date),
                cancellationToken))
            .FirstOrDefault();

        LeaveRequest? leave = null;
        if (leaveOverride is not null &&
            leaveOverride.EmployeeId == employeeId &&
            leaveOverride.Status == LeaveRequestStatus.Approved &&
            leaveOverride.StartDate <= date &&
            leaveOverride.EndDate >= date)
        {
            leave = leaveOverride;
        }
        else
        {
            leave = (await leaves.ListAsync(
                    new Specification<LeaveRequest>().Where(x =>
                        x.EmployeeId == employeeId &&
                        x.Status == LeaveRequestStatus.Approved &&
                        x.StartDate <= date &&
                        x.EndDate >= date &&
                        (!excludedLeaveRequestId.HasValue || x.Id != excludedLeaveRequestId.Value)),
                    cancellationToken))
                .FirstOrDefault();
        }

        if (matchingAssignments.Count == 0)
        {
            var Status = holiday is not null
                ? AttendanceDayStatus.Holiday
                : leave is not null
                    ? AttendanceDayStatus.Leave
                    : AttendanceDayStatus.Unscheduled;

            return new AttendanceScheduleResolution(
                null,
                Status,
                leave?.Id,
                holiday?.Id,
                timeZoneId,
                null,
                null,
                0);
        }

        var shift = await shifts.GetByIdAsync(matchingAssignments[0].WorkShiftId, cancellationToken)
            ?? throw new ConflictException("shift_missing", "Assigned shift does not exist.");

        var status = holiday is not null
            ? AttendanceDayStatus.Holiday
            : !shift.IsWorkingDay(date.DayOfWeek)
                ? AttendanceDayStatus.Weekend
                : leave is not null
                    ? AttendanceDayStatus.Leave
                    : AttendanceDayStatus.Absent;

        var scheduledStart = zones.ToUtc(date, shift.StartTime, timeZoneId);
        var endDate = shift.CrossesMidnight ? date.AddDays(1) : date;
        var scheduledEnd = zones.ToUtc(endDate, shift.EndTime, timeZoneId);

        return new AttendanceScheduleResolution(
            shift,
            status,
            leave?.Id,
            holiday?.Id,
            timeZoneId,
            scheduledStart,
            scheduledEnd,
            shift.ScheduledMinutes);
    }

    public static (int Late, int Early) CalculateVariance(
        AttendanceScheduleResolution schedule,
        DateTimeOffset? checkIn,
        DateTimeOffset? checkOut)
    {
        if (schedule.Shift is null || schedule.Shift.IsFlexible)
        {
            return (0, 0);
        }

        var late = checkIn.HasValue && schedule.ScheduledStartUtc.HasValue
            ? (int)Math.Max(
                0,
                (checkIn.Value - schedule.ScheduledStartUtc.Value).TotalMinutes -
                schedule.Shift.GraceLateMinutes)
            : 0;

        var early = checkOut.HasValue && schedule.ScheduledEndUtc.HasValue
            ? (int)Math.Max(
                0,
                (schedule.ScheduledEndUtc.Value - checkOut.Value).TotalMinutes -
                schedule.Shift.GraceEarlyLeaveMinutes)
            : 0;

        return (late, early);
    }

    public static void Apply(AttendanceRecord record, AttendanceScheduleResolution schedule)
    {
        var shift = schedule.Shift;

        record.ApplySchedule(
            shift?.Id,
            shift?.ShiftCode,
            shift?.NameAr,
            schedule.TimeZoneId,
            schedule.ScheduledStartUtc,
            schedule.ScheduledEndUtc,
            shift?.BreakMinutes ?? 0,
            shift?.GraceLateMinutes ?? 0,
            shift?.GraceEarlyLeaveMinutes ?? 0,
            shift?.IsFlexible ?? false,
            schedule.ScheduledMinutes,
            schedule.Status,
            schedule.LeaveRequestId,
            schedule.HolidayId);

        if (!record.CheckInAtUtc.HasValue && !record.CheckOutAtUtc.HasValue)
        {
            return;
        }

        var variance = CalculateVariance(schedule, record.CheckInAtUtc, record.CheckOutAtUtc);
        record.RecordTimes(
            record.CheckInAtUtc,
            record.CheckOutAtUtc,
            record.Source,
            variance.Late,
            variance.Early,
            record.Notes);
    }
}

public sealed class LeaveDayCalculationService(
    IReadRepository<EmployeeShiftAssignment, Guid> assignments,
    IReadRepository<WorkShift, Guid> shifts,
    IReadRepository<Holiday, Guid> holidays)
{
    public async Task<decimal> CalculateAsync(
        Guid employeeId,
        LeaveDayCountingMethod method,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken)
    {
        if (end < start)
        {
            throw new ConflictException(
                "leave_dates_invalid",
                "Leave end date cannot be before start date.");
        }

        if (method == LeaveDayCountingMethod.CalendarDays)
        {
            return end.DayNumber - start.DayNumber + 1;
        }

        var matchingAssignments = await assignments.ListAsync(
            new Specification<EmployeeShiftAssignment>().Where(x =>
                x.EmployeeId == employeeId &&
                x.IsActive &&
                x.EffectiveFrom <= end &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= start)),
            cancellationToken);

        var shiftMap = (await shifts.ListAsync(cancellationToken: cancellationToken))
            .ToDictionary(x => x.Id);

        var holidayList = await holidays.ListAsync(
            new Specification<Holiday>().Where(x =>
                x.IsActive &&
                x.StartDate <= end &&
                x.EndDate >= start),
            cancellationToken);

        decimal days = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var matches = matchingAssignments
                .Where(x =>
                    x.EffectiveFrom <= date &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= date))
                .ToArray();

            if (matches.Length > 1)
            {
                throw new ConflictException(
                    "shift_overlap",
                    "Overlapping shift assignments prevent leave-day calculation.");
            }

            if (matches.Length == 0)
            {
                throw new ConflictException(
                    "shift_assignment_required_for_leave",
                    "A shift assignment is required to calculate working-day leave.");
            }

            if (!shiftMap.TryGetValue(matches[0].WorkShiftId, out var shift))
            {
                throw new ConflictException("shift_missing", "Assigned shift does not exist.");
            }

            if (!shift.IsWorkingDay(date.DayOfWeek))
            {
                continue;
            }

            if (holidayList.Any(x => x.Contains(date)))
            {
                continue;
            }

            days++;
        }

        return days;
    }
}
