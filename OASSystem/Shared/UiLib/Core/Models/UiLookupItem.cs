namespace OAS.UiLib.Core.Models;

public sealed record UiLookupItem(
    string Value,
    string PrimaryText,
    string? SecondaryText = null,
    string? IconCssClass = null,
    bool Disabled = false);
