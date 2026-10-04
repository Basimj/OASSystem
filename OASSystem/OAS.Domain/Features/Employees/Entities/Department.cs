using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class Department : AuditableEntity<Guid>
{
    private Department() { }

    private Department(Guid id, string departmentCode, string nameAr, string? nameEn, Guid? parentDepartmentId, Guid? managerEmployeeId, bool isActive, string? notes)
    {
        if (id == Guid.Empty) throw new DomainException("Department id is required.");
        if (string.IsNullOrWhiteSpace(departmentCode)) throw new DomainException("Department code is required.");
        if (departmentCode.Trim().Length > 32) throw new DomainException("Department code cannot exceed 32 characters.");
        Id = id;
        DepartmentCode = departmentCode.Trim();
        Update(nameAr, nameEn, parentDepartmentId, managerEmployeeId, isActive, notes);
    }

    public string DepartmentCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public Guid? ParentDepartmentId { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Department Create(Guid id, string departmentCode, string nameAr, string? nameEn = null, Guid? parentDepartmentId = null, Guid? managerEmployeeId = null, bool isActive = true, string? notes = null)
        => new(id, departmentCode, nameAr, nameEn, parentDepartmentId, managerEmployeeId, isActive, notes);

    public void Update(string nameAr, string? nameEn, Guid? parentDepartmentId, Guid? managerEmployeeId, bool isActive, string? notes)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("Arabic department name is required.");
        nameAr = nameAr.Trim();
        if (nameAr.Length > 150) throw new DomainException("Arabic department name cannot exceed 150 characters.");
        nameEn = Normalize(nameEn);
        if (nameEn is { Length: > 150 }) throw new DomainException("English department name cannot exceed 150 characters.");
        notes = Normalize(notes);
        if (notes is { Length: > 500 }) throw new DomainException("Department notes cannot exceed 500 characters.");
        if (parentDepartmentId == Id) throw new DomainException("A department cannot be its own parent.");

        NameAr = nameAr;
        NameEn = nameEn;
        ParentDepartmentId = parentDepartmentId;
        ManagerEmployeeId = managerEmployeeId;
        IsActive = isActive;
        Notes = notes;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
