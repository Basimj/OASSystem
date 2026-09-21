namespace OAS.UiLib.Components.Accounting.Accounts;

public sealed record AccountTreeNode(
    Guid Id,
    string Code,
    string NameAr,
    bool IsActive,
    IReadOnlyList<AccountTreeNode> Children);
