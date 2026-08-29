namespace OAS.Client.Database.State;

public sealed class DatabaseProfileSelectionState
{
    public const string HeaderName = "X-OAS-Database-Profile";
    public string? SelectedProfileKey { get; set; }
}
