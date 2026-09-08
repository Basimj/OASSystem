namespace OAS.UiLib.Core.Models;

public sealed record UiApplicationTabItem(
    Guid Id,
    string Title,
    string Href,
    string IconCssClass,
    bool IsActive,
    bool CanClose = true);
