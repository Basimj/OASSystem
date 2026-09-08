namespace OAS.UiLib.Core.Models;

/// <summary>
/// Describes an application module shown by the shared OAS shell.
/// Route ownership stays in the application; the UI library only renders and tracks the workspace.
/// </summary>
public sealed record UiShellModuleItem(
    string Key,
    string Title,
    string Href,
    string IconCssClass,
    bool AdministratorOnly = false,
    bool DividerBefore = false);
