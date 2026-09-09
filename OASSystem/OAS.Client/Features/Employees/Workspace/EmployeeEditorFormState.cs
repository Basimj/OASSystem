using System.ComponentModel.DataAnnotations;

namespace OAS.Client.Features.Employees.Workspace;

public sealed class EmployeeEditorFormState
{
    [Required(ErrorMessage = "كود الموظف مطلوب.")]
    [MaxLength(32, ErrorMessage = "كود الموظف يجب ألا يتجاوز 32 حرفًا.")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم الأول مطلوب.")]
    [MaxLength(100, ErrorMessage = "الاسم الأول يجب ألا يتجاوز 100 حرف.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم العائلة مطلوب.")]
    [MaxLength(100, ErrorMessage = "اسم العائلة يجب ألا يتجاوز 100 حرف.")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(32, ErrorMessage = "رقم الهاتف يجب ألا يتجاوز 32 حرفًا.")]
    public string? Phone { get; set; }

    [MaxLength(100, ErrorMessage = "المسمى الوظيفي يجب ألا يتجاوز 100 حرف.")]
    public string? JobTitle { get; set; }

    public string HireDateText { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "الملاحظات يجب ألا تتجاوز 1000 حرف.")]
    public string? Notes { get; set; }

    public bool IsSalesperson { get; set; }
    public bool IsTechnician { get; set; }
    public bool IsCommissionEligible { get; set; }
    public bool IsActive { get; set; } = true;

    public void Reset()
    {
        EmployeeCode = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Phone = string.Empty;
        JobTitle = string.Empty;
        HireDateText = string.Empty;
        Notes = string.Empty;
        IsSalesperson = false;
        IsTechnician = false;
        IsCommissionEligible = false;
        IsActive = true;
    }
}
