using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Payroll;

public sealed class PayrollPolicy : AuditableEntity<Guid>
{
    private PayrollPolicy() { }
    private PayrollPolicy(Guid id, string code, string nameAr, DateOnly effectiveFrom, DateOnly? effectiveTo,
        PayrollProrationMethod proration, PayrollDailyRateMethod daily, PayrollHourlyRateMethod hourly,
        bool requireAttendance, bool requireFullPayment,
        Guid? absenceComponentId, Guid? lateComponentId, Guid? earlyComponentId, Guid? unpaidLeaveComponentId, Guid? overtimeComponentId, string? notes)
    {
        if (id == Guid.Empty) throw new DomainException("Payroll policy id is required.");
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("Payroll policy code and name are required.");
        Id = id; PolicyCode = code.Trim(); Status = PayrollPolicyStatus.Draft;
        UpdateDraft(nameAr, effectiveFrom, effectiveTo, proration, daily, hourly, requireAttendance, requireFullPayment,
            absenceComponentId, lateComponentId, earlyComponentId, unpaidLeaveComponentId, overtimeComponentId, notes);
    }
    public string PolicyCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public PayrollPolicyStatus Status { get; private set; }
    public PayrollProrationMethod ProrationMethod { get; private set; }
    public PayrollDailyRateMethod DailyRateMethod { get; private set; }
    public PayrollHourlyRateMethod HourlyRateMethod { get; private set; }
    public bool RequireApprovedAttendance { get; private set; }
    public bool RequireFullPaymentBeforeRunClose { get; private set; }
    public Guid? AbsenceDeductionComponentId { get; private set; }
    public Guid? LateDeductionComponentId { get; private set; }
    public Guid? EarlyLeaveDeductionComponentId { get; private set; }
    public Guid? UnpaidLeaveComponentId { get; private set; }
    public Guid? OvertimeComponentId { get; private set; }
    public string? Notes { get; private set; }
    public string? ActivatedBy { get; private set; }
    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PayrollPolicy Create(Guid id, string code, string nameAr, DateOnly effectiveFrom, DateOnly? effectiveTo,
        PayrollProrationMethod proration, PayrollDailyRateMethod daily, PayrollHourlyRateMethod hourly,
        bool requireAttendance, bool requireFullPayment, Guid? absenceComponentId, Guid? lateComponentId,
        Guid? earlyComponentId, Guid? unpaidLeaveComponentId, Guid? overtimeComponentId, string? notes)
        => new(id, code, nameAr, effectiveFrom, effectiveTo, proration, daily, hourly, requireAttendance, requireFullPayment,
            absenceComponentId, lateComponentId, earlyComponentId, unpaidLeaveComponentId, overtimeComponentId, notes);

    public void UpdateDraft(string nameAr, DateOnly effectiveFrom, DateOnly? effectiveTo,
        PayrollProrationMethod proration, PayrollDailyRateMethod daily, PayrollHourlyRateMethod hourly,
        bool requireAttendance, bool requireFullPayment, Guid? absenceComponentId, Guid? lateComponentId,
        Guid? earlyComponentId, Guid? unpaidLeaveComponentId, Guid? overtimeComponentId, string? notes)
    {
        if (Status != PayrollPolicyStatus.Draft) throw new DomainException("Only draft payroll policies can be edited.");
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("Payroll policy name is required.");
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom) throw new DomainException("Payroll policy effective dates are invalid.");
        if (!Enum.IsDefined(proration) || !Enum.IsDefined(daily) || !Enum.IsDefined(hourly)) throw new DomainException("Payroll policy method is invalid.");
        NameAr = nameAr.Trim(); EffectiveFrom = effectiveFrom; EffectiveTo = effectiveTo;
        ProrationMethod = proration; DailyRateMethod = daily; HourlyRateMethod = hourly;
        RequireApprovedAttendance = requireAttendance; RequireFullPaymentBeforeRunClose = requireFullPayment;
        AbsenceDeductionComponentId = Normalize(absenceComponentId); LateDeductionComponentId = Normalize(lateComponentId);
        EarlyLeaveDeductionComponentId = Normalize(earlyComponentId); UnpaidLeaveComponentId = Normalize(unpaidLeaveComponentId);
        OvertimeComponentId = Normalize(overtimeComponentId); Notes = N(notes);
        if (NameAr.Length > 150 || Notes is { Length: > 500 }) throw new DomainException("Payroll policy text is too long.");
    }
    public void Activate(string? actor, DateTimeOffset at)
    {
        if (Status != PayrollPolicyStatus.Draft) throw new DomainException("Only draft payroll policies can be activated.");
        Status = PayrollPolicyStatus.Active; ActivatedBy = N(actor); ActivatedAtUtc = at;
    }
    public void Supersede(DateOnly effectiveTo)
    {
        if (Status != PayrollPolicyStatus.Active || effectiveTo < EffectiveFrom) throw new DomainException("Payroll policy cannot be superseded on this date.");
        EffectiveTo = effectiveTo; Status = PayrollPolicyStatus.Superseded;
    }
    public void Cancel() { if (Status != PayrollPolicyStatus.Draft) throw new DomainException("Only draft payroll policies can be cancelled."); Status = PayrollPolicyStatus.Cancelled; }
    private static Guid? Normalize(Guid? value) => value is { } id && id != Guid.Empty ? id : null;
    private static string? N(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
