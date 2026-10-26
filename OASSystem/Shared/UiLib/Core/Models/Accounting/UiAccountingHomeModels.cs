namespace OAS.UiLib.Core.Models.Accounting;

public sealed record UiAccountingHomeMetric(
    string Label,
    string Value,
    string IconCssClass,
    string Accent = "primary",
    string? Meta = null,
    string? Href = null);

public sealed record UiAccountingHomeTile(
    string Title,
    string Description,
    string Href,
    string IconCssClass,
    string Accent = "primary",
    bool IsSecondary = false);

public sealed record UiAccountingHomeSection(
    string Title,
    string IconCssClass,
    IReadOnlyList<UiAccountingHomeTile> Tiles,
    string Accent = "primary");

public sealed record UiAccountingHomeAttentionItem(
    string Title,
    string Meta,
    string Severity,
    string Href);

public sealed record UiAccountingHomeActivityItem(
    string Reference,
    string Title,
    string Meta,
    string Tone,
    string IconCssClass,
    string Href);
