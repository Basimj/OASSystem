using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Leave;

public sealed class EmployeeLeaveBalance : AuditableEntity<Guid>
{
    private EmployeeLeaveBalance()
    {
    }
    private EmployeeLeaveBalance(Guid id, Guid emp, Guid type, short year, decimal opening, decimal accrued, decimal used, decimal adjustment)
    {
        Id=id;
        EmployeeId=emp;
        LeaveTypeId=type;
        LeaveYear=year;
        SetValues(opening, accrued, used, adjustment);
    }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public short LeaveYear { get; private set; }
    public decimal OpeningBalanceDays { get; private set; }
    public decimal AccruedDays { get; private set; }
    public decimal UsedDays { get; private set; }
    public decimal AdjustmentDays { get; private set; }
    public decimal SettledDays { get; private set; }
    public decimal AvailableDays=>OpeningBalanceDays+AccruedDays+AdjustmentDays-UsedDays-SettledDays;
    public byte[] RowVersion { get; private set; } = [];
    public static EmployeeLeaveBalance Create(Guid id, Guid employeeId, Guid typeId, short year, decimal opening=0, decimal accrued=0, decimal used=0, decimal adjustment=0)
    {
        if (id==Guid.Empty||employeeId==Guid.Empty||typeId==Guid.Empty)throw new DomainException("Leave balance identity is required.");
        return new(id, employeeId, typeId, year, opening, accrued, used, adjustment);
    }
    public void SetValues(decimal opening, decimal accrued, decimal used, decimal adjustment)
    {
        if (opening<0||accrued<0||used<0)throw new DomainException("Leave balance values cannot be negative except adjustments.");
        OpeningBalanceDays=opening;
        AccruedDays=accrued;
        UsedDays=used;
        AdjustmentDays=adjustment;
    }
    public void Consume(decimal days, bool allowNegative=false)
    {
        if (days<=0)throw new DomainException("Leave days must be positive.");
        if (!allowNegative&&AvailableDays<days)throw new DomainException("Insufficient leave balance.");
        UsedDays+=days;
    }
    public void Restore(decimal days)
    {
        if (days<=0||days>UsedDays)throw new DomainException("Leave balance restore amount is invalid.");
        UsedDays-=days;
    }
    public void AddAccrual(decimal days)
    {
        if (days<0)throw new DomainException("Accrual cannot be negative.");
        AccruedDays+=days;
    }
    public void Adjust(decimal days)=>AdjustmentDays+=days;
    public void Settle(decimal days)
    {
        if (days <= 0 || days > AvailableDays) throw new DomainException("Leave settlement amount is invalid.");
        SettledDays += days;
    }
    public void RestoreSettlement(decimal days)
    {
        if (days <= 0 || days > SettledDays) throw new DomainException("Leave settlement restore amount is invalid.");
        SettledDays -= days;
    }
}
