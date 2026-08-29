namespace OAS.UiLib.Core.Models;

public sealed record UiDataColumn<TItem>(string Header, Func<TItem, string?> ValueSelector);
