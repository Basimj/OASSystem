namespace OAS.Client.Identity.Users.Workspace;

public sealed class UserEditorFormModel
{
    private readonly Action _onChanged;
    private string _userName = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string? _email;
    private string _phoneNumber = string.Empty;
    private bool _isActive = true;
    private bool _suppressNotifications;

    public UserEditorFormModel(Action onChanged) => _onChanged = onChanged;

    public string UserName
    {
        get => _userName;
        set => Set(ref _userName, value ?? string.Empty);
    }

    public string FirstName
    {
        get => _firstName;
        set => Set(ref _firstName, value ?? string.Empty);
    }

    public string LastName
    {
        get => _lastName;
        set => Set(ref _lastName, value ?? string.Empty);
    }

    public string? Email
    {
        get => _email;
        set => Set(ref _email, value);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set => Set(ref _phoneNumber, value ?? string.Empty);
    }

    public bool IsActive
    {
        get => _isActive;
        set => Set(ref _isActive, value);
    }

    public void Load(string userName, string firstName, string lastName, string? email, string? phoneNumber, bool isActive)
    {
        _suppressNotifications = true;
        _userName = userName;
        _firstName = firstName;
        _lastName = lastName;
        _email = email;
        _phoneNumber = phoneNumber ?? string.Empty;
        _isActive = isActive;
        _suppressNotifications = false;
    }

    private void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        if (!_suppressNotifications) _onChanged();
    }
}
