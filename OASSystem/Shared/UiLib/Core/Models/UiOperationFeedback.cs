namespace OAS.UiLib.Core.Models;

public sealed record UiOperationFeedback(bool Succeeded, string? Message = null, string? Value = null);
