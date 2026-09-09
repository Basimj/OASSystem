namespace OAS.UiLib.Core.Models;

public sealed record UiShellNavigationGroup(
    string Key,
    string Title,
    string IconCssClass,
    IReadOnlyList<string> ModuleKeys,
    bool DefaultExpanded = true);
