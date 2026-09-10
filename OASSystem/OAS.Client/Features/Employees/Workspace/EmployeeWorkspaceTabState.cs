using OAS.Contracts.Features.Employees;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Features.Employees.Workspace;

public sealed class EmployeeWorkspaceTabState
{
    private EmployeeEditorSnapshot? _baseline;

    public EmployeeWorkspaceTabState(Guid? employeeId)
    {
        EmployeeId = employeeId;
        IsEditMode = employeeId is null;
    }

    public Guid TabId { get; } = Guid.NewGuid();
    public Guid? EmployeeId { get; private set; }
    public bool IsNew => EmployeeId is null;
    public bool IsInitialized { get; private set; }
    public bool IsLoading { get; set; }
    public bool IsSaving { get; set; }
    public bool IsEditMode { get; private set; }
    public bool HasPersistedPhoto { get; private set; }
    public string? RowVersion { get; private set; }
    public string? UserAccountDisplayName { get; private set; }
    public string? UserAccountUserName { get; private set; }
    public string? UserAccountEmail { get; private set; }
    public UiEmployeeEditorModel Form { get; } = new();

    public byte[]? PendingImageContent { get; private set; }
    public string? PendingImageContentType { get; private set; }
    public string? PendingImageFileName { get; private set; }
    public string? PendingImagePreviewUrl { get; private set; }
    public bool IsImageRemovalPending { get; private set; }
    public long ImageRevision { get; private set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public bool HasImageChanges => PendingImageContent is not null || IsImageRemovalPending;
    public bool IsDirty => (_baseline is not null && !_baseline.Equals(Capture())) || HasImageChanges;
    public bool IsUserAccountLinkLocked => !IsNew && _baseline?.UserAccountId is not null;

    public string? EffectiveImageUrl => PendingImagePreviewUrl
        ?? (IsImageRemovalPending || !HasPersistedPhoto || EmployeeId is not Guid employeeId
            ? null
            : $"api/employees/{employeeId:D}/image?v={ImageRevision}");

    public bool ShowRemoveImage => PendingImageContent is not null || (!IsImageRemovalPending && HasPersistedPhoto);

    public string Title
    {
        get
        {
            if (IsNew) return "موظف جديد";
            var name = $"{Form.FirstName} {Form.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? "موظف" : name;
        }
    }

    public void InitializeNew(int employeeNumber)
    {
        Form.Reset(employeeNumber);
        EmployeeId = null;
        RowVersion = null;
        HasPersistedPhoto = false;
        UserAccountDisplayName = null;
        UserAccountUserName = null;
        UserAccountEmail = null;
        IsEditMode = true;
        IsInitialized = true;
        ClearImageChanges();
        _baseline = Capture();
    }

    public void Load(EmployeeDto employee, bool preserveImageChanges = false)
    {
        var pendingContent = preserveImageChanges ? PendingImageContent : null;
        var pendingType = preserveImageChanges ? PendingImageContentType : null;
        var pendingFileName = preserveImageChanges ? PendingImageFileName : null;
        var pendingPreview = preserveImageChanges ? PendingImagePreviewUrl : null;
        var pendingRemoval = preserveImageChanges && IsImageRemovalPending;

        EmployeeId = employee.Id;
        RowVersion = employee.RowVersion;
        HasPersistedPhoto = !string.IsNullOrWhiteSpace(employee.Photo);
        UserAccountDisplayName = employee.UserAccountDisplayName;
        UserAccountUserName = employee.UserAccountUserName;
        UserAccountEmail = employee.UserAccountEmail;

        Form.EmployeeNumber = employee.EmployeeNumber;
        Form.FirstName = employee.FirstName;
        Form.LastName = employee.LastName;
        Form.Phone = employee.Phone ?? string.Empty;
        Form.Email = employee.Email ?? string.Empty;
        Form.Country = employee.Country ?? string.Empty;
        Form.Governorate = employee.Governorate ?? string.Empty;
        Form.City = employee.City ?? string.Empty;
        Form.PostalCode = employee.PostalCode ?? string.Empty;
        Form.ResidentialAddress = employee.ResidentialAddress ?? string.Empty;
        Form.JobTitleId = employee.JobTitleId;
        Form.HireDate = employee.HireDate;
        Form.IsCommissionEligible = employee.IsCommissionEligible;
        Form.IsActive = employee.IsActive;
        Form.UserAccountId = employee.UserAccountId;

        IsInitialized = true;
        ImageRevision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _baseline = Capture();

        ClearImageChanges();
        if (preserveImageChanges)
        {
            PendingImageContent = pendingContent;
            PendingImageContentType = pendingType;
            PendingImageFileName = pendingFileName;
            PendingImagePreviewUrl = pendingPreview;
            IsImageRemovalPending = pendingRemoval;
        }

        IsEditMode = preserveImageChanges && HasImageChanges;
    }

    public void BeginEdit()
    {
        if (!IsInitialized || IsNew) return;
        IsEditMode = true;
    }

    public void Revert()
    {
        if (_baseline is null) return;
        Apply(_baseline);
        ClearImageChanges();
        IsEditMode = IsNew;
    }

    public void StageImage(byte[] content, string contentType, string fileName, string previewUrl)
    {
        PendingImageContent = content;
        PendingImageContentType = contentType;
        PendingImageFileName = fileName;
        PendingImagePreviewUrl = previewUrl;
        IsImageRemovalPending = false;
    }

    public void StageImageRemoval()
    {
        PendingImageContent = null;
        PendingImageContentType = null;
        PendingImageFileName = null;
        PendingImagePreviewUrl = null;
        IsImageRemovalPending = true;
    }

    public void ClearImageDraft() => ClearImageChanges();

    public void AcceptImageSave(bool hasImage)
    {
        ClearImageChanges();
        HasPersistedPhoto = hasImage;
        ImageRevision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public void CompleteSave(EmployeeDto employee) => Load(employee);

    private void ClearImageChanges()
    {
        PendingImageContent = null;
        PendingImageContentType = null;
        PendingImageFileName = null;
        PendingImagePreviewUrl = null;
        IsImageRemovalPending = false;
    }

    private EmployeeEditorSnapshot Capture() => new(
        Form.FirstName,
        Form.LastName,
        Form.Phone ?? string.Empty,
        Form.Email ?? string.Empty,
        Form.Country ?? string.Empty,
        Form.Governorate ?? string.Empty,
        Form.City ?? string.Empty,
        Form.PostalCode ?? string.Empty,
        Form.ResidentialAddress ?? string.Empty,
        Form.JobTitleId,
        Form.HireDate,
        Form.IsCommissionEligible,
        Form.IsActive,
        Form.UserAccountId);

    private void Apply(EmployeeEditorSnapshot snapshot)
    {
        Form.FirstName = snapshot.FirstName;
        Form.LastName = snapshot.LastName;
        Form.Phone = snapshot.Phone;
        Form.Email = snapshot.Email;
        Form.Country = snapshot.Country;
        Form.Governorate = snapshot.Governorate;
        Form.City = snapshot.City;
        Form.PostalCode = snapshot.PostalCode;
        Form.ResidentialAddress = snapshot.ResidentialAddress;
        Form.JobTitleId = snapshot.JobTitleId;
        Form.HireDate = snapshot.HireDate;
        Form.IsCommissionEligible = snapshot.IsCommissionEligible;
        Form.IsActive = snapshot.IsActive;
        Form.UserAccountId = snapshot.UserAccountId;
    }

    private sealed record EmployeeEditorSnapshot(
        string FirstName,
        string LastName,
        string Phone,
        string Email,
        string Country,
        string Governorate,
        string City,
        string PostalCode,
        string ResidentialAddress,
        Guid? JobTitleId,
        DateOnly? HireDate,
        bool IsCommissionEligible,
        bool IsActive,
        Guid? UserAccountId);
}
