namespace OAS.UiLib.Core.Models;

public sealed record UiContextMenuItem(
    string Key,
    string Text,
    string? IconCssClass = null,
    bool IsActive = false,
    bool Disabled = false);
