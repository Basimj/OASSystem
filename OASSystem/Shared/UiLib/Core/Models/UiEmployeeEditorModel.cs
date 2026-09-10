namespace OAS.UiLib.Core.Models;

public sealed class UiEmployeeEditorModel
{
    public int EmployeeNumber { get; set; }
    public string EmployeeCode => EmployeeNumber > 0 ? EmployeeNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Country { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? ResidentialAddress { get; set; }
    public Guid? JobTitleId { get; set; }
    public DateOnly? HireDate { get; set; }
    public bool IsCommissionEligible { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? UserAccountId { get; set; }

    public void Reset(int employeeNumber = 0)
    {
        EmployeeNumber = employeeNumber;
        FirstName = string.Empty;
        LastName = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        Country = string.Empty;
        Governorate = string.Empty;
        City = string.Empty;
        PostalCode = string.Empty;
        ResidentialAddress = string.Empty;
        JobTitleId = null;
        HireDate = null;
        IsCommissionEligible = false;
        IsActive = true;
        UserAccountId = null;
    }
}
