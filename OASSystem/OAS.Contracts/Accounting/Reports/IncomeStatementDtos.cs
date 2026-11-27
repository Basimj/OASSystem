using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.Reports;
public sealed record IncomeStatementFilterDto(DateOnly FromDate, DateOnly ToDate, Guid? FiscalYearId = null, Guid? FiscalPeriodId = null, Guid? CostCenterId = null);
public sealed record IncomeStatementAccountDto(Guid AccountId, string Code, string Name, Guid? ParentAccountId, byte Level, AccountClass AccountClass, FinancialStatementCategory? Category);
public sealed record IncomeStatementMovementDto(Guid AccountId, int Year, int Month, decimal Amount);
public sealed record IncomeStatementReportDto(DateOnly FromDate, DateOnly ToDate, IReadOnlyList<IncomeStatementAccountDto> Accounts, IReadOnlyList<IncomeStatementMovementDto> Movements);
