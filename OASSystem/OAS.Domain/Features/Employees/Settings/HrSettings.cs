using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Settings;

public sealed class HrSettings : AuditableEntity<Guid>
{
    public static readonly Guid SingletonId = Guid.Parse("6e599352-4de1-4f62-b410-f903eec1a601");
    private HrSettings()
    {
    }
    private HrSettings(string? timeZoneId, byte leaveYearStartMonth, bool requireAttendanceApproval)
    {
        Id = SingletonId;
        Update(timeZoneId, leaveYearStartMonth, requireAttendanceApproval);
    }
    public string? TimeZoneId { get; private set; }
    public byte LeaveYearStartMonth { get; private set; }
    public bool RequireAttendanceApproval { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static HrSettings Create(string? timeZoneId, byte leaveYearStartMonth = 1, bool requireAttendanceApproval = true)         => new(timeZoneId, leaveYearStartMonth, requireAttendanceApproval);
    public void Update(string? timeZoneId, byte leaveYearStartMonth, bool requireAttendanceApproval)
    {
        timeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim();
        if (timeZoneId is
        {
            Length: > 128
        }
        ) throw new DomainException("HR time zone id cannot exceed 128 characters.");
        if (leaveYearStartMonth is < 1 or > 12) throw new DomainException("Leave year start month must be between 1 and 12.");
        TimeZoneId = timeZoneId;
        LeaveYearStartMonth = leaveYearStartMonth;
        RequireAttendanceApproval = requireAttendanceApproval;
    }
}
