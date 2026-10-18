namespace OAS.Contracts.Sales.Returns;

/// <summary>Non-mutating readiness result. A posting command still rechecks under transaction.</summary>
public sealed record SalesReturnPostingReadinessDto(
    Guid SalesReturnId,
    bool CanPost,
    IReadOnlyList<SalesReturnPostingIssueDto> Issues);

public sealed record SalesReturnPostingIssueDto(string Code, string Message);
