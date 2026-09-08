namespace OAS.UiLib.Core.Models;

public sealed record UiWorkspaceTabItem(
    Guid Id,
    string Title,
    bool IsActive,
    bool IsDirty = false,
    bool CanClose = true);
