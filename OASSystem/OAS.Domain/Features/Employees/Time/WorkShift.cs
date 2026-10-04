using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Time;

public sealed class WorkShift : AuditableEntity<Guid>
{
    private WorkShift() { }
    private WorkShift(Guid id, string code, string nameAr, string? nameEn, TimeOnly start, TimeOnly end, int breakMinutes, int graceLateMinutes, int graceEarlyLeaveMinutes, byte workingDaysMask, bool isFlexible, bool isActive, string? notes)
    {
        if (id == Guid.Empty) throw new DomainException("Shift id is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Shift code is required.");
        Id = id; ShiftCode = code.Trim();
        Update(nameAr, nameEn, start, end, breakMinutes, graceLateMinutes, graceEarlyLeaveMinutes, workingDaysMask, isFlexible, isActive, notes);
    }

    public string ShiftCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int BreakMinutes { get; private set; }
    public int GraceLateMinutes { get; private set; }
    public int GraceEarlyLeaveMinutes { get; private set; }
    public byte WorkingDaysMask { get; private set; }
    public bool IsFlexible { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public bool CrossesMidnight => EndTime < StartTime;
    public int RawDurationMinutes => (int)((EndTime.ToTimeSpan() - StartTime.ToTimeSpan() + (CrossesMidnight ? TimeSpan.FromDays(1) : TimeSpan.Zero)).TotalMinutes);
    public int ScheduledMinutes => RawDurationMinutes - BreakMinutes;

    public static WorkShift Create(Guid id, string code, string nameAr, string? nameEn, TimeOnly start, TimeOnly end, int breakMinutes, int graceLateMinutes, int graceEarlyLeaveMinutes, byte workingDaysMask, bool isFlexible, bool isActive, string? notes)
        => new(id, code, nameAr, nameEn, start, end, breakMinutes, graceLateMinutes, graceEarlyLeaveMinutes, workingDaysMask, isFlexible, isActive, notes);

    public void Update(string nameAr, string? nameEn, TimeOnly start, TimeOnly end, int breakMinutes, int graceLateMinutes, int graceEarlyLeaveMinutes, byte workingDaysMask, bool isFlexible, bool isActive, string? notes)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("Arabic shift name is required.");
        if (start == end) throw new DomainException("Shift start and end times cannot be equal.");
        if (breakMinutes < 0 || graceLateMinutes < 0 || graceEarlyLeaveMinutes < 0) throw new DomainException("Shift minute values cannot be negative.");
        var crosses = end < start;
        var duration = (int)((end.ToTimeSpan() - start.ToTimeSpan() + (crosses ? TimeSpan.FromDays(1) : TimeSpan.Zero)).TotalMinutes);
        if (breakMinutes >= duration) throw new DomainException("Break minutes must be less than shift duration.");
        if (workingDaysMask is 0 or > 127) throw new DomainException("Working days mask is invalid.");
        nameAr = nameAr.Trim(); nameEn = Normalize(nameEn); notes = Normalize(notes);
        if (nameAr.Length > 150 || nameEn is { Length: > 150 }) throw new DomainException("Shift name is too long.");
        if (notes is { Length: > 500 }) throw new DomainException("Shift notes cannot exceed 500 characters.");
        NameAr = nameAr; NameEn = nameEn; StartTime = start; EndTime = end; BreakMinutes = breakMinutes;
        GraceLateMinutes = graceLateMinutes; GraceEarlyLeaveMinutes = graceEarlyLeaveMinutes; WorkingDaysMask = workingDaysMask;
        IsFlexible = isFlexible; IsActive = isActive; Notes = notes;
    }
    public void SetActive(bool active) => IsActive = active;
    public bool IsWorkingDay(DayOfWeek day) => (WorkingDaysMask & DayMask(day)) != 0;
    private static byte DayMask(DayOfWeek day) => day switch { DayOfWeek.Monday => 1, DayOfWeek.Tuesday => 2, DayOfWeek.Wednesday => 4, DayOfWeek.Thursday => 8, DayOfWeek.Friday => 16, DayOfWeek.Saturday => 32, DayOfWeek.Sunday => 64, _ => 0 };
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
