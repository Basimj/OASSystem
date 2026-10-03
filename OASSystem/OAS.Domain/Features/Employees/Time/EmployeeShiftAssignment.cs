using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Time;

public sealed class EmployeeShiftAssignment : AuditableEntity<Guid>
{
    private EmployeeShiftAssignment()
    {
    }
    private EmployeeShiftAssignment(Guid id, Guid employeeId, Guid workShiftId, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isActive)
    {
        Id=id;
        EmployeeId=employeeId;
        WorkShiftId=workShiftId;
        Update(workShiftId, effectiveFrom, effectiveTo, isActive);
    }
    public Guid EmployeeId { get; private set; }
    public Guid WorkShiftId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static EmployeeShiftAssignment Create(Guid id, Guid employeeId, Guid shiftId, DateOnly from, DateOnly? to=null, bool active=true)
    {
        if (id==Guid.Empty||employeeId==Guid.Empty||shiftId==Guid.Empty) throw new DomainException("Assignment identity is invalid.");
        return new(id, employeeId, shiftId, from, to, active);
    }
    public void Update(Guid shiftId, DateOnly from, DateOnly? to, bool active)
    {
        if (shiftId==Guid.Empty) throw new DomainException("Shift is required.");
        if (to.HasValue&&to.Value<from) throw new DomainException("Assignment end date cannot be before start date.");
        WorkShiftId=shiftId;
        EffectiveFrom=from;
        EffectiveTo=to;
        IsActive=active;
    }
    public void SetActive(bool active)=>IsActive=active;
    public bool Overlaps(DateOnly from, DateOnly? to)
    {
        var thisEnd=EffectiveTo??DateOnly.MaxValue;
        var otherEnd=to??DateOnly.MaxValue;
        return EffectiveFrom<=otherEnd&&from<=thisEnd;
    }
}
