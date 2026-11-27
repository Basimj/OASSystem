using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.Accounts;

public sealed record FinancialStatementCategoryChange(Guid AccountId, string RowVersion, FinancialStatementCategory? Category);
public sealed record UpdateFinancialStatementCategoriesRequest(IReadOnlyList<FinancialStatementCategoryChange> Changes);
public sealed record UpdateFinancialStatementCategoriesResult(int UpdatedCount);
