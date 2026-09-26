using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using ContractPaymentMethod=OAS.Contracts.Accounting.Enums.PaymentMethod;
using ContractPartyType=OAS.Contracts.Accounting.Enums.SettlementPartyType;
using ContractRateType=OAS.Contracts.Accounting.Enums.ExchangeRateType;
using ContractRateSource=OAS.Contracts.Accounting.Enums.ExchangeRateSource;
using ContractStatus=OAS.Contracts.Accounting.Enums.ReceiptVoucherStatus;
namespace OAS.Application.Accounting.ReceiptVouchers.Mapping;
public sealed class ReceiptVoucherMapper
{
 public ReceiptVoucherDto ToRead(ReceiptVoucher source)=>ToRead(source,source.Lines);
 public ReceiptVoucherDto ToRead(ReceiptVoucher source,IEnumerable<ReceiptVoucherLine> sourceLines)=>new(
  source.Id,source.VoucherNumber,source.VoucherDate,source.BaseCurrencyId,source.BaseCurrencyCodeSnapshot,source.BaseCurrencyDecimalPlacesSnapshot,source.BaseTotalAmount,
  (ContractStatus)(byte)source.Status,source.Description,source.JournalEntryId,source.CreatedBy,source.CreatedAtUtc,source.PostedBy,source.PostedAtUtc,Convert.ToBase64String(source.RowVersion),sourceLines.OrderBy(x=>x.LineNumber).Select(ToLineDto).ToArray());
 public static ReceiptVoucherLineDto ToLineDto(ReceiptVoucherLine x)=>new(
  x.Id,x.ReceiptVoucherId,x.LineNumber,x.AccountId,
  x.PartyType.HasValue?(ContractPartyType?)(byte)x.PartyType.Value:null,x.CustomerId,x.SupplierId,x.EmployeeId,x.PartyNameSnapshot,x.CounterpartyAccountId,
  x.PaymentMethod.HasValue?(ContractPaymentMethod?)(byte)x.PaymentMethod.Value:null,x.CashAccountId,x.BankAccountId,x.SettlementAccountId,
  x.CurrencyId,x.CurrencyCodeSnapshot,x.CurrencySymbolSnapshot,x.CurrencyDecimalPlacesSnapshot,x.Amount,x.ExchangeRate,x.ExchangeRateDate,
  x.ExchangeRateType.HasValue?(ContractRateType?)(byte)x.ExchangeRateType.Value:null,x.ExchangeRateSource.HasValue?(ContractRateSource?)(byte)x.ExchangeRateSource.Value:null,x.BaseAmount,
  x.ReferenceNumber,x.ReferenceDate,x.ReferenceType,x.ReferenceId,x.Description);
}
