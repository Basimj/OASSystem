using OAS.Contracts.Accounting.Enums;
namespace OAS.Application.Features.Employees.Abstractions;

public interface IHRTimeZoneService
{
    TimeZoneInfo Resolve(string? timeZoneId);
    DateTimeOffset ToUtc(DateOnly localDate, TimeOnly localTime, string? timeZoneId);
    DateOnly ToLocalDate(DateTimeOffset utc, string? timeZoneId);
}

public interface IEmployeeHrOperationLock
{
    Task AcquireAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

public sealed record LoanSettlementRequest(DateOnly Date,Guid EmployeeId,Guid CurrencyId,decimal Amount,PaymentMethod PaymentMethod,Guid? CashAccountId,Guid? BankAccountId,Guid? SettlementAccountId,decimal? ExchangeRate,ExchangeRateType ExchangeRateType,string? ReferenceNumber,string ReferenceType,Guid ReferenceId,string? Description);
public interface IHREmployeeLoanAccountingPort
{
    Task<Guid> DisburseAsync(LoanSettlementRequest request,CancellationToken cancellationToken=default);
    Task<Guid> ReceiveRepaymentAsync(LoanSettlementRequest request,CancellationToken cancellationToken=default);
}
