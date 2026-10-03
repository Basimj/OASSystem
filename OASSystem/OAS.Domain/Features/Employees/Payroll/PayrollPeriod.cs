using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Payroll;

public sealed class PayrollPeriod : AuditableEntity<Guid>
{
    private PayrollPeriod() { }
    private PayrollPeriod(Guid id, short year, byte month, DateOnly startDate, DateOnly endDate)
    {
        if (id == Guid.Empty || year < 1900 || month is < 1 or > 12 || endDate < startDate) throw new DomainException("Payroll period data is invalid.");
        Id = id; Year = year; Month = month; StartDate = startDate; EndDate = endDate; PeriodCode = $"PAY-{year:0000}-{month:00}"; Status = PayrollPeriodStatus.Open;
    }
    public string PeriodCode { get; private set; } = string.Empty;
    public short Year { get; private set; }
    public byte Month { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public PayrollPeriodStatus Status { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset? LockedAtUtc { get; private set; }
    public string? ClosedBy { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static PayrollPeriod Create(Guid id, short year, byte month, DateOnly startDate, DateOnly endDate) => new(id, year, month, startDate, endDate);
    public void Lock(string? actor, DateTimeOffset at) { if (Status != PayrollPeriodStatus.Open) throw new DomainException("Only open payroll periods can be locked."); Status = PayrollPeriodStatus.Locked; LockedBy=N(actor); LockedAtUtc=at; }
    public void Reopen() { if (Status != PayrollPeriodStatus.Locked) throw new DomainException("Only locked payroll periods can be reopened."); Status=PayrollPeriodStatus.Open; LockedBy=null; LockedAtUtc=null; }
    public void Close(string? actor, DateTimeOffset at) { if (Status != PayrollPeriodStatus.Locked) throw new DomainException("Only locked payroll periods can be closed."); Status=PayrollPeriodStatus.Closed; ClosedBy=N(actor); ClosedAtUtc=at; }
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
