using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Features.Employees.Services;

public sealed class HRTimeZoneService : IHRTimeZoneService
{
    public TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        var requestedId = timeZoneId.Trim();
        if (TryResolve(requestedId, out var zone))
        {
            return zone;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(requestedId, out var windowsId) &&
            TryResolve(windowsId, out zone))
        {
            return zone;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(requestedId, out var ianaId) &&
            TryResolve(ianaId, out zone))
        {
            return zone;
        }

        throw new ConflictException(
            "hr_timezone_invalid",
            "The configured HR time zone is invalid on this server.");
    }

    private static bool TryResolve(string timeZoneId, out TimeZoneInfo zone)
    {
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            zone = null!;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            zone = null!;
            return false;
        }
    }
    public DateTimeOffset ToUtc(DateOnly localDate, TimeOnly localTime, string? timeZoneId)
    {
        var zone = Resolve(timeZoneId);
        var local = DateTime.SpecifyKind(localDate.ToDateTime(localTime), DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local)) throw new ConflictException("hr_local_time_invalid", "The local shift time falls inside a daylight-saving gap.");
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }
    public DateOnly ToLocalDate(DateTimeOffset utc, string? timeZoneId) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, Resolve(timeZoneId)).DateTime);
}

public sealed class EmployeeHrOperationLock(OasDbContext dbContext) : IEmployeeHrOperationLock
{
    public async Task AcquireAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty) throw new ArgumentException("Employee id is required.", nameof(employeeId));
        if (dbContext.Database.CurrentTransaction is null) throw new InvalidOperationException("HR employee operation lock requires an active database transaction.");
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [hr].[Employees] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {employeeId}", cancellationToken);
    }
}

public sealed class EmployeeLoanAccountingPort(
    IRepository<PaymentVoucher, Guid> paymentVouchers,
    IRepository<ReceiptVoucher, Guid> receiptVouchers,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    IReadRepository<Currency, Guid> currencies,
    IVoucherSettlementResolver settlements,
    IAccountingDocumentPostingService posting,
    ISequenceNumberGenerator sequences,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IHREmployeeLoanAccountingPort
{
    public async Task<Guid> DisburseAsync(LoanSettlementRequest request, CancellationToken ct)
    {
        var settlement = await settlements.ResolveAsync(request.Date, SettlementPartyType.Employee, null, null, request.EmployeeId, null, null,
            (PaymentMethod)(byte)request.PaymentMethod, request.CashAccountId, request.BankAccountId, request.SettlementAccountId, request.CurrencyId,
            request.Amount, request.ExchangeRate, (ExchangeRateType)(byte)request.ExchangeRateType, request.ReferenceNumber, ct);
        var (settings, baseCurrency) = await GetBaseCurrencyAsync(ct);
        var id = Guid.NewGuid();
        var number = await NextUniquePaymentNumberAsync(request.Date.Year, ct);
        var voucher = PaymentVoucher.CreateSettlementDocument(id, number, request.Date, settings.BaseCurrencyId, baseCurrency.Code, baseCurrency.DecimalPlaces, settlement.BaseAmount, request.Description);
        var line = PaymentVoucherLine.CreateSettlement(Guid.NewGuid(), id, 1, settlement.PartyType, null, null, request.EmployeeId, settlement.PartyNameSnapshot,
            settlement.CounterpartyAccountId, settlement.PaymentMethod, settlement.CashAccountId, settlement.BankAccountId, settlement.SettlementAccountId,
            settlement.CurrencyId, settlement.CurrencyCode, settlement.CurrencySymbol, settlement.CurrencyDecimalPlaces, settlement.Amount, settlement.ExchangeRate,
            settlement.ExchangeRateDate, settlement.ExchangeRateType, settlement.ExchangeRateSource, settlement.BaseAmount, request.ReferenceNumber, request.Date,
            request.ReferenceType, request.ReferenceId, request.Description);
        voucher.AddLine(line); voucher.Approve([line]);
        var userId = CurrentUserGuid(); var now = timeProvider.GetUtcNow().UtcDateTime;
        var journalId = await posting.PostPaymentVoucherAsync(voucher, [line], userId, now, ct);
        voucher.SetJournalEntry(journalId); voucher.Post(userId, now, [line]);
        await paymentVouchers.AddAsync(voucher, ct);
        return id;
    }

    public async Task<Guid> ReceiveRepaymentAsync(LoanSettlementRequest request, CancellationToken ct)
    {
        var settlement = await settlements.ResolveAsync(request.Date, SettlementPartyType.Employee, null, null, request.EmployeeId, null, null,
            (PaymentMethod)(byte)request.PaymentMethod, request.CashAccountId, request.BankAccountId, request.SettlementAccountId, request.CurrencyId,
            request.Amount, request.ExchangeRate, (ExchangeRateType)(byte)request.ExchangeRateType, request.ReferenceNumber, ct);
        var (settings, baseCurrency) = await GetBaseCurrencyAsync(ct);
        var id = Guid.NewGuid();
        var number = await NextUniqueReceiptNumberAsync(request.Date.Year, ct);
        var voucher = ReceiptVoucher.CreateSettlementDocument(id, number, request.Date, settings.BaseCurrencyId, baseCurrency.Code, baseCurrency.DecimalPlaces, settlement.BaseAmount, request.Description);
        var line = ReceiptVoucherLine.CreateSettlement(Guid.NewGuid(), id, 1, settlement.PartyType, null, null, request.EmployeeId, settlement.PartyNameSnapshot,
            settlement.CounterpartyAccountId, settlement.PaymentMethod, settlement.CashAccountId, settlement.BankAccountId, settlement.SettlementAccountId,
            settlement.CurrencyId, settlement.CurrencyCode, settlement.CurrencySymbol, settlement.CurrencyDecimalPlaces, settlement.Amount, settlement.ExchangeRate,
            settlement.ExchangeRateDate, settlement.ExchangeRateType, settlement.ExchangeRateSource, settlement.BaseAmount, request.ReferenceNumber, request.Date,
            request.ReferenceType, request.ReferenceId, request.Description);
        voucher.AddLine(line); voucher.Approve([line]);
        var userId = CurrentUserGuid(); var now = timeProvider.GetUtcNow().UtcDateTime;
        var journalId = await posting.PostReceiptVoucherAsync(voucher, [line], userId, now, ct);
        voucher.SetJournalEntry(journalId); voucher.Post(userId, now, [line]);
        await receiptVouchers.AddAsync(voucher, ct);
        return id;
    }

    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException();
    private async Task<(AccountingSettings Settings, Currency Currency)> GetBaseCurrencyAsync(CancellationToken ct)
    {
        var settings = await settingsRepository.GetByIdAsync(AccountingSettings.SingletonId, ct) ?? throw new ConflictException("accounting_settings_required", "Accounting settings and base currency must be configured first.");
        var currency = await currencies.GetByIdAsync(settings.BaseCurrencyId, ct) ?? throw new ConflictException("base_currency_missing", "Configured base currency does not exist.");
        return (settings, currency);
    }
    private async Task<string> NextUniquePaymentNumberAsync(int year, CancellationToken ct)
    {
        for (var i=0;i<100;i++) { var n=$"PV-{year:0000}-{await sequences.NextAsync($"PaymentVoucher-{year}",ct):000000}"; if(await paymentVouchers.CountAsync(new Specification<PaymentVoucher>().Where(x=>x.VoucherNumber==n),ct)==0)return n; }
        throw new ConflictException("payment_voucher_number_duplicate","Could not allocate a unique payment voucher number.");
    }
    private async Task<string> NextUniqueReceiptNumberAsync(int year, CancellationToken ct)
    {
        for (var i=0;i<100;i++) { var n=$"RV-{year:0000}-{await sequences.NextAsync($"ReceiptVoucher-{year}",ct):000000}"; if(await receiptVouchers.CountAsync(new Specification<ReceiptVoucher>().Where(x=>x.VoucherNumber==n),ct)==0)return n; }
        throw new ConflictException("receipt_voucher_number_duplicate","Could not allocate a unique receipt voucher number.");
    }
}
