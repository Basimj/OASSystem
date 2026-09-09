namespace OAS.Client.Features.Employees.Workspace;

public sealed class EmployeeEditorWorkspaceState
{
    public Guid? EmployeeId { get; private set; }
    public bool IsInitialized { get; private set; }
    public string? RowVersion { get; set; }
    public Guid? UserAccountId { get; set; }
    public string ActiveSection { get; set; } = "basic";
    public EmployeeEditorFormState Form { get; } = new();

    public bool Matches(Guid? employeeId) => IsInitialized && EmployeeId == employeeId;

    public void Begin(Guid? employeeId)
    {
        EmployeeId = employeeId;
        IsInitialized = false;
        RowVersion = null;
        UserAccountId = null;
        ActiveSection = "basic";
        Form.Reset();
    }

    public void MarkInitialized() => IsInitialized = true;

    public void Clear()
    {
        EmployeeId = null;
        IsInitialized = false;
        RowVersion = null;
        UserAccountId = null;
        ActiveSection = "basic";
        Form.Reset();
    }
}
