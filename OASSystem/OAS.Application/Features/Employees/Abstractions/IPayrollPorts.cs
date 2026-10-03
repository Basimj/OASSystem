using OAS.Domain.Features.Employees.Payroll;
using OAS.Domain.Features.Employees.EndOfService;

namespace OAS.Application.Features.Employees.Abstractions;

public sealed record ApprovedCommissionItem(Guid SourceDocumentId, DateOnly Date, decimal Amount, string Description, string? DebitPostingRole, string? CreditPostingRole);
public interface IApprovedCommissionSource
{
    Task<IReadOnlyList<ApprovedCommissionItem>> GetApprovedAsync(Guid employeeId, DateOnly from, DateOnly to, Guid currencyId, CancellationToken cancellationToken = default);
    Task MarkConsumedAsync(Guid sourceDocumentId, Guid employeePayrollId, CancellationToken cancellationToken = default);
}

public sealed record PayrollPostingLine(Guid EmployeeId, string EmployeeName, decimal Amount, byte ComponentType, string DebitPostingRole, string CreditPostingRole, string Description, Guid? SourceDocumentId);
public sealed record PayrollPostingRequest(Guid PayrollRunId, string PayrollRunCode, DateOnly PostingDate, Guid CurrencyId, IReadOnlyList<PayrollPostingLine> Lines);
public sealed record EmployeePayrollBaseAmounts(Guid EmployeePayrollId, decimal BaseGross, decimal BaseDeductions, decimal BaseEmployerContributions, decimal BaseNet);
public sealed record PayrollPostingResult(Guid JournalEntryId, Guid BaseCurrencyId, string BaseCurrencyCode, byte BaseCurrencyDecimalPlaces, decimal ExchangeRate, DateOnly ExchangeRateDate, byte ExchangeRateType, byte ExchangeRateSource, decimal BaseGross, decimal BaseDeductions, decimal BaseEmployerContributions, decimal BaseNet, IReadOnlyList<EmployeePayrollBaseAmounts> Employees);
public interface IHRPayrollAccountingPort
{
    Task<PayrollPostingResult> PostAsync(PayrollPostingRequest request, IReadOnlyList<EmployeePayroll> employeePayrolls, CancellationToken cancellationToken = default);
}

public sealed record SalaryPaymentPortRequest(Guid EmployeePayrollId, Guid EmployeeId, string EmployeeName, DateOnly PaymentDate, decimal Amount, byte PaymentMethod, Guid? CashAccountId, Guid? BankAccountId, Guid SettlementAccountId, decimal? ExchangeRate, byte ExchangeRateType, string? ReferenceNumber, string? Description);
public sealed record SalaryPaymentPortResult(Guid PaymentVoucherId, Guid PaymentVoucherLineId, Guid PaymentAllocationId, decimal SourceBaseAmount, decimal TargetBaseAmount, decimal RealizedExchangeDifferenceBase);
public interface IHRSalaryPaymentPort
{
    Task<SalaryPaymentPortResult> PayAsync(SalaryPaymentPortRequest request, CancellationToken cancellationToken = default);
}

public sealed record EndOfServiceCalculationInput(Guid EmployeeId, DateOnly HireDate, DateOnly LastWorkingDate, decimal BasicSalary, decimal RequestedBenefitAmount, decimal? RequestedLeaveDailyRate);
public interface IEndOfServiceCalculationPolicy
{
    Task<decimal> CalculateBenefitAsync(EndOfServiceCalculationInput input, CancellationToken cancellationToken = default);
    Task<decimal> CalculateLeaveDailyRateAsync(EndOfServiceCalculationInput input, CancellationToken cancellationToken = default);
}

public sealed record EndOfServicePostingLine(Guid EmployeeId, decimal Amount, bool IsDeduction, string DebitPostingRole, string CreditPostingRole, string Description, Guid? SourceDocumentId);
public sealed record EndOfServicePostingRequest(Guid SettlementId, string SettlementCode, DateOnly PostingDate, Guid CurrencyId, IReadOnlyList<EndOfServicePostingLine> Lines);
public sealed record EndOfServicePostingResult(Guid JournalEntryId, decimal ExchangeRate, DateOnly ExchangeRateDate, byte ExchangeRateType, byte ExchangeRateSource, decimal BaseGross, decimal BaseNet);
public interface IHREndOfServiceAccountingPort
{
    Task<EndOfServicePostingResult> PostAsync(EndOfServicePostingRequest request, CancellationToken cancellationToken = default);
}

public interface IHREmployeeIdentityPort
{
    Task DisableLinkedUserAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

public sealed record PayrollCalculatedSegment(Guid? ContractId,string? ContractCode,Guid SalaryStructureId,string SalaryStructureCode,DateOnly From,DateOnly To,decimal BasicRate,decimal ProrationFactor);
public sealed record PayrollCalculatedLine(Guid? SalaryStructureId,Guid? SalaryComponentId,string? ComponentCode,string ComponentName,byte ComponentType,byte SourceType,string? SourceModule,string? SourceDocumentType,Guid? SourceDocumentId,DateOnly? SourceDate,decimal? Quantity,decimal? Rate,decimal Amount,string? DebitPostingRole,string? CreditPostingRole,string? Description);
public sealed record PayrollCalculatedEmployee(Guid EmployeeId,DateOnly CoverageFrom,DateOnly CoverageTo,string EmployeeCode,string EmployeeName,string? JobTitle,string? Department,Guid? ContractId,string? ContractCode,Guid CurrencyId,string CurrencyCode,string? CurrencySymbol,byte CurrencyDecimalPlaces,decimal ConfiguredBasicSalary,decimal CalculatedBasicSalary,decimal Gross,decimal Deductions,decimal EmployerContributions,decimal Net,IReadOnlyList<PayrollCalculatedSegment> Segments,IReadOnlyList<PayrollCalculatedLine> Lines);
public sealed record PayrollCalculationIssue(Guid? EmployeeId,string? EmployeeCode,string? EmployeeName,string Code,string Message,byte Severity);
public sealed record PayrollCalculationBatch(IReadOnlyList<PayrollCalculatedEmployee> Employees,IReadOnlyList<PayrollCalculationIssue> Issues);
public interface IEmployeePayrollCalculationService
{
    Task<PayrollCalculationBatch> PrevalidateAndCalculateAsync(PayrollRun run, PayrollPeriod period, PayrollPolicy policy, bool calculate, CancellationToken cancellationToken = default);
}

public sealed record EndOfServicePaymentPortRequest(Guid SettlementId,Guid EmployeeId,string EmployeeName,DateOnly PaymentDate,decimal Amount,byte PaymentMethod,Guid? CashAccountId,Guid? BankAccountId,Guid SettlementAccountId,decimal? ExchangeRate,byte ExchangeRateType,string? ReferenceNumber,string? Description);
public sealed record EndOfServicePaymentPortResult(Guid PaymentVoucherId,Guid PaymentVoucherLineId,Guid PaymentAllocationId);
public interface IHREndOfServicePaymentPort
{
    Task<EndOfServicePaymentPortResult> PayAsync(EndOfServicePaymentPortRequest request,CancellationToken cancellationToken=default);
}
