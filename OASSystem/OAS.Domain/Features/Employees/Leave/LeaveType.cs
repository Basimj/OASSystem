using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Leave;

public sealed class LeaveType : AuditableEntity<Guid>
{
    private LeaveType()
    {
    }
    private LeaveType(Guid id, string code, string nameAr, string? nameEn, bool isPaid, bool requiresBalance, LeaveAccrualMethod accrual, LeaveDayCountingMethod counting, decimal annual, decimal maxCarry, bool prorate, bool active)
    {
        Id=id;
        LeaveTypeCode=code.Trim();
        Update(nameAr, nameEn, isPaid, requiresBalance, accrual, counting, annual, maxCarry, prorate, active);
    }
    public string LeaveTypeCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public bool IsPaid { get; private set; }
    public bool RequiresBalance { get; private set; }
    public LeaveAccrualMethod AccrualMethod { get; private set; }
    public LeaveDayCountingMethod DayCountingMethod { get; private set; }
    public decimal AnnualEntitlementDays { get; private set; }
    public decimal MaximumCarryForwardDays { get; private set; }
    public bool ProrateOnHire { get; private set; }
    public bool IsEncashableOnTermination { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static LeaveType Create(Guid id, string code, string nameAr, string? nameEn, bool isPaid, bool requiresBalance, LeaveAccrualMethod accrual, LeaveDayCountingMethod counting, decimal annual, decimal maxCarry, bool prorate, bool active)
    {
        if (id==Guid.Empty||string.IsNullOrWhiteSpace(code))throw new DomainException("Leave type identity is required.");
        return new(id, code, nameAr, nameEn, isPaid, requiresBalance, accrual, counting, annual, maxCarry, prorate, active);
    }
    public void Update(string nameAr, string? nameEn, bool isPaid, bool requiresBalance, LeaveAccrualMethod accrual, LeaveDayCountingMethod counting, decimal annual, decimal maxCarry, bool prorate, bool active)
    {
        if (string.IsNullOrWhiteSpace(nameAr))throw new DomainException("Leave type name is required.");
        if (!Enum.IsDefined(accrual)||!Enum.IsDefined(counting))throw new DomainException("Leave type policy is invalid.");
        if (annual<0||maxCarry<0)throw new DomainException("Leave entitlement values cannot be negative.");
        NameAr=nameAr.Trim();
        NameEn=N(nameEn);
        IsPaid=isPaid;
        RequiresBalance=requiresBalance;
        AccrualMethod=accrual;
        DayCountingMethod=counting;
        AnnualEntitlementDays=annual;
        MaximumCarryForwardDays=maxCarry;
        ProrateOnHire=prorate;
        IsActive=active;
        if (NameAr.Length>150||NameEn is
        {
            Length:>150
        }
        )throw new DomainException("Leave type name is too long.");
    }
    public void SetActive(bool active)=>IsActive=active;
    public void SetTerminationEncashment(bool enabled)=>IsEncashableOnTermination=enabled;
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
