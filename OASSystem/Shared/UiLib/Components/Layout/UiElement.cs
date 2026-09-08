using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace OAS.UiLib.Components.Layout;

/// <summary>
/// Low-level reusable markup primitive for feature views that need semantic HTML
/// while keeping rendering primitives centralized in UiLib.
/// Prefer the higher-level UiLib components whenever one matches the use case.
/// </summary>
public sealed class UiElement : ComponentBase
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "div", "span", "section", "header", "nav", "main", "form", "label",
        "table", "thead", "tbody", "tr", "th", "td", "p", "h1", "h2", "h3", "ul", "li",
        "button", "input", "textarea", "strong", "i"
    };

    [Parameter, EditorRequired]
    public string Tag { get; set; } = "div";

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter]
    public EventCallback<ChangeEventArgs> OnChange { get; set; }

    [Parameter]
    public EventCallback<ChangeEventArgs> OnInput { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var tag = AllowedTags.Contains(Tag) ? Tag.ToLowerInvariant() : "div";

        builder.OpenElement(0, tag);
        if (AdditionalAttributes is not null)
            builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (Value is not null && !string.Equals(tag, "textarea", StringComparison.Ordinal))
            builder.AddAttribute(2, "value", Value);

        if (OnClick.HasDelegate)
            builder.AddAttribute(3, "onclick", OnClick);

        if (ValueChanged.HasDelegate || OnChange.HasDelegate)
        {
            builder.AddAttribute(4, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, async args =>
            {
                if (ValueChanged.HasDelegate)
                    await ValueChanged.InvokeAsync(args.Value?.ToString());
                if (OnChange.HasDelegate)
                    await OnChange.InvokeAsync(args);
            }));
        }

        if (OnInput.HasDelegate)
            builder.AddAttribute(5, "oninput", OnInput);

        if (string.Equals(tag, "textarea", StringComparison.Ordinal))
        {
            builder.AddContent(6, Value);
        }
        else if (!string.Equals(tag, "input", StringComparison.Ordinal))
        {
            builder.AddContent(6, ChildContent);
        }

        builder.CloseElement();
    }
}
